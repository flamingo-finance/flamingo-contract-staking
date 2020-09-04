using System;
using System.Collections.Generic;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework.Services.Neo;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] _currentRateHeightPrefix = new byte[] { 0x01, 0x01 };        
        private static readonly byte[] _currentUintStackProfitPrefix = new byte[] { 0x01, 0x02 };
        private static readonly BigInteger StartHeight = 10000;
        delegate object DyncCall(string method, object[] args);
        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return false;
            }
            else if (Runtime.Trigger == TriggerType.Application) 
            {
                byte[] ExecutingScriptHash = ExecutionEngine.ExecutingScriptHash;
                if (method == "staking")
                {
                    return Staking((byte[])args[0], (BigInteger)args[1], (byte[])args[2]);
                }
                else if (method == "refund")
                {
                    return Refund((byte[])args[0], (BigInteger)args[1], (byte[])args[2]);
                }
                else if (method == "claimflm")
                {
                    return ClaimFLM((byte[])args[0], (byte[])args[1], ExecutingScriptHash);
                }
                else if (method == "checkflm")
                {
                    return CheckFLM((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "addadmin")
                {
                    return AddAdmin((byte[])args[0]);
                }
                else if (method == "removeadmin")
                {
                    return RemoveAdmin((byte[])args[0]);
                }
                else if (method == "setflmaddress")
                {
                    return SetFlmAddress((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "getflmaddress")
                {
                    return GetFlmAddress();
                }
                else if (method == "addasset")
                {
                    return AddAsset((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "removeasset")
                {
                    return RemoveAsset((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "setshareamount")
                {
                    return SetCurrentShareAmount((byte[])args[0], (BigInteger)args[1], (byte[])args[2]);
                }

                else if (method == "getshareamount")
                {
                    return GetCurrentShareAmount((byte[])args[0]);
                }
                else if (method == "isadmin")
                {
                    return IsAdmin((byte[])args[0]);
                }
                else if (method == "isinwhitelist")
                {
                    return IsInWhiteList((byte[])args[0]);
                }

                else if (method == "getuintprofit")
                {
                    var assetId = (byte[])args[0];

                    var currentTotalStakingAmount = GetCurrentTotalAmount(assetId);
                    var currentShareAmount = GetCurrentShareAmount(assetId);
                    return currentShareAmount / currentTotalStakingAmount;
                }
                else if (method == "getstackingamount")
                {
                    var fromAddress = (byte[])args[0];
                    var assetId = (byte[])args[1];

                    byte[] key = assetId.Concat(fromAddress);
                    var result = Storage.Get(key);

                    if (result.Length != 0)
                    {
                        StakingReocrd stakingRecord = (StakingReocrd)result.Deserialize();
                        return stakingRecord.amount;
                    }
                    return 0;
                }
                else if (method == "calculateprofit") 
                {
                    return CalculateProfit((byte[])args[0], (byte[])args[1]);
                }
            }
            return false;
        }

        public static bool Staking(byte[] fromAddress, BigInteger amount, byte[] assetId) 
        {
            if (!IsInWhiteList(assetId) || assetId.Length != 20) return false; //throw exception when release
            object[] Params = new object[]
            {
                fromAddress,
                ExecutionEngine.ExecutingScriptHash,
                amount
            };
            if (!(bool)((DyncCall)assetId.ToDelegate())("transfer", Params)) return false; //throw exception when release
            BigInteger currentHeight = Blockchain.GetHeight();
            byte[] key = assetId.Concat(fromAddress);
            var result = Storage.Get(key);
            BigInteger currentProfit = 0;
            UpdateStackRecord(assetId);
            if (result.Length != 0)
            {
                StakingReocrd stakingRecord = (StakingReocrd)result.Deserialize();
                currentProfit = SettleProfit(stakingRecord.height, stakingRecord.amount, assetId) + stakingRecord.Profit;
                amount += stakingRecord.amount;
            }
            SaveUserStaking(fromAddress, amount, assetId, currentHeight, currentProfit, key);
            return true;
        }        

        public static bool Refund(byte[] fromAddress, BigInteger amount, byte[] assetId) 
        {
            //提现检查
            if (!Runtime.CheckWitness(fromAddress)) return false;
            BigInteger currentHeight = Blockchain.GetHeight();
            byte[] key = assetId.Concat(fromAddress);
            var result = Storage.Get(key);
            if (result.Length == 0) return false;
            StakingReocrd stakingRecord = (StakingReocrd)result.Deserialize();
            //Nep5转账
            object[] Params = new object[]
            {
                ExecutionEngine.ExecutingScriptHash,
                fromAddress,
                amount
            };
            DyncCall nep5Contract = (DyncCall)assetId.ToDelegate();
            if (!(bool)nep5Contract("transfer", Params)) return false; //throw exception when release
            if (stakingRecord.amount < amount || !(stakingRecord.fromAddress.Equals(fromAddress)) || !(stakingRecord.assetId.Equals(assetId)))
            {
                return false;
            }
            else             
            {
                BigInteger remainAmount = (stakingRecord.amount - amount);
                UpdateStackRecord(assetId);
                //收益结算
                BigInteger currentProfit = SettleProfit(stakingRecord.height, stakingRecord.amount, assetId) + stakingRecord.Profit;
                SaveUserStaking(fromAddress, remainAmount, assetId, currentHeight, currentProfit, key);
            }
            return true;
        }

        public static BigInteger CalculateProfit(byte[] fromaddress, byte[] assetId)
        {
            UpdateStackRecord(assetId);
            byte[] key = assetId.Concat(fromaddress);
            var result = Storage.Get(key);
            if (result.Length == 0) return -1;
            StakingReocrd staking = (StakingReocrd)result.Deserialize();
            BigInteger currentProfit = SettleProfit(staking.height, staking.amount, assetId) + staking.Profit;
            Runtime.Notify(staking.Profit);
            return currentProfit;
        }

        public static bool ClaimFLM(byte[] fromAddress, byte[] assetId, byte[] callingScript) 
        {
            if (!Runtime.CheckWitness(fromAddress)) return false;
            byte[] key = assetId.Concat(fromAddress);
            StakingReocrd stakingReocrd = (StakingReocrd)Storage.Get(key).Deserialize();
            if (!stakingReocrd.fromAddress.Equals(fromAddress))
            {
                return false;
            }
            var profitAmount = stakingReocrd.Profit;
            SaveUserStaking(fromAddress, stakingReocrd.amount, stakingReocrd.assetId, stakingReocrd.height, 0, key);
            if (!MintFLM(fromAddress, profitAmount, callingScript))             
            {
                return false;
            }
            return true;
        }

        public static BigInteger CheckFLM(byte[] fromAddress, byte[] assetId)         
        {
            byte[] key = assetId.Concat(fromAddress);
            var result = Storage.Get(key);
            if (result.Length != 0) 
            {
                StakingReocrd stakingReocrd = (StakingReocrd)result.Deserialize();
                return stakingReocrd.Profit;
            }
            return 0;
        }

        public static BigInteger SettleProfit(BigInteger recordHeight, BigInteger amount, byte[] assetId) 
        {
            BigInteger currentHeight = Blockchain.GetHeight();
            BigInteger MinusProfit = GetHistoryUintStackProfitSum(assetId, recordHeight);
            BigInteger SumProfit = GetHistoryUintStackProfitSum(assetId, currentHeight);
            BigInteger currentProfit = (SumProfit - MinusProfit) * amount;
            return currentProfit;
        }
    }
}
