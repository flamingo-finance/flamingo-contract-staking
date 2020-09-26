using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;
using System;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] _currentRateTimeStampPrefix = new byte[] { 0x01, 0x01 };        
        private static readonly byte[] _currentUintStackProfitPrefix = new byte[] { 0x01, 0x02 };
        private static readonly uint StartStakingTimeStamp = 1601114400;
        private static readonly uint StartClaimTimeStamp = 1601269200;
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
                else if (method == "claimFLM")
                {
                    return ClaimFLM((byte[])args[0], (byte[])args[1], ExecutingScriptHash);
                }
                else if (method == "addAdmin")
                {
                    return AddAdmin((byte[])args[0]);
                }
                else if (method == "removeAdmin")
                {
                    return RemoveAdmin((byte[])args[0]);
                }
                else if (method == "setFLMAddress")
                {
                    return SetFlmAddress((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "addAsset")
                {
                    return AddAsset((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "removeAsset")
                {
                    return RemoveAsset((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "setShareAmount")
                {
                    return SetCurrentShareAmount((byte[])args[0], (BigInteger)args[1], (byte[])args[2]);
                }
                else if (method == "setOwner")
                {
                    return SetOwner((byte[])args[0]);
                }
                else if (method == "upgradeStart")
                {
                    return UpgradeStart();
                }

                else if (method == "getFLMAddress")
                {
                    return GetFlmAddress();
                }
                else if (method == "checkFLM")
                {
                    return CheckFLM((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "getShareAmount")
                {
                    return GetCurrentShareAmount((byte[])args[0]);
                }
                else if (method == "isAdmin")
                {
                    return IsAdmin((byte[])args[0]);
                }
                else if (method == "isInWhiteList")
                {
                    return IsInWhiteList((byte[])args[0]);
                }
                else if (method == "getUintProfit")
                {
                    return GetUintProfit((byte[])args[0]);
                }
                else if (method == "getStakingAmount")
                {
                    return GetStakingAmount((byte[])args[0], (byte[])args[1]);
                }
                else if (method == "getCurrentTotalAmount")
                {
                    return GetCurrentTotalAmount((byte[])args[0]);
                }
                else if (method == "pause")
                {
                    return Pause((byte[])args[0]);
                }
                else if (method == "unpause")
                {
                    return Unpause((byte[])args[0]);
                }
                else if (method == "pauseStaking")
                {
                    return PauseStaking((byte[])args[0]);
                }
                else if (method == "pauseRefund")
                {
                    return PauseRefund((byte[])args[0]);
                }
                else if (method == "unpauseStaking")
                {
                    return unpauseStaking((byte[])args[0]);
                }
                else if (method == "unpauseRefund") 
                {
                    return unpauseRefund((byte[])args[0]);
                }
                else if (method == "isPaused")
                {
                    return IsPaused();
                }
                else if (method == "getOwner")
                {
                    return GetOwner();
                }
                else if (method == "upgrade")
                {
                    return Upgrade((byte[])args[0], (byte[])args[1], (byte)args[2], (int)args[3], (string)args[4], (string)args[5], (string)args[6], (string)args[7], (string)args[8]);
                }
            }
            return false;
        }

        [DisplayName("getUintProfit")]
        public static object GetUintProfit(byte[] assetId)
        {
            if (assetId.Length != 20 || !IsInWhiteList(assetId))
            {
                return 0;
            }
            return GetCurrentUintStackProfit(assetId);
        }

        [DisplayName("staking")]
        public static bool Staking(byte[] fromAddress, BigInteger amount, byte[] assetId) 
        {
            if (IsStakingPaused()) return false;
            if (!IsInWhiteList(assetId) || assetId.Length != 20 || CheckWhetherSelf(fromAddress) || amount <= 0) return false;
            object[] Params = new object[]
            {
                fromAddress,
                ExecutionEngine.ExecutingScriptHash,
                amount
            };
            BigInteger currentTimeStamp = GetCurrentTimeStamp();
            if (!CheckIfStakingStart(currentTimeStamp)) return false;
            if (!(bool)((DyncCall)assetId.ToDelegate())("transfer", Params)) throw new Exception(); //throw exception when release
            byte[] key = assetId.Concat(fromAddress);
            byte[] result = Storage.Get(key);
            BigInteger currentProfit = 0;
            UpdateStackRecord(assetId, currentTimeStamp);
            if (result.Length != 0)
            {
                StakingReocrd stakingRecord = (StakingReocrd)result.Deserialize();
                currentProfit = SettleProfit(stakingRecord.timeStamp, stakingRecord.amount, assetId) + stakingRecord.Profit;
                amount += stakingRecord.amount;
            }
            SaveUserStaking(fromAddress, amount, assetId, currentTimeStamp, currentProfit, key);
            return true;
        }

        [DisplayName("refund")]
        public static bool Refund(byte[] fromAddress, BigInteger amount, byte[] assetId) 
        {
            if (IsRefundPaused()) return false;
            //提现检查
            if (!Runtime.CheckWitness(fromAddress)) return false;
            BigInteger currentTimeStamp = GetCurrentTimeStamp();            
            byte[] key = assetId.Concat(fromAddress);
            var result = Storage.Get(key);
            if (result.Length == 0) return false;
            StakingReocrd stakingRecord = (StakingReocrd)result.Deserialize();
            if (stakingRecord.amount < amount || !(stakingRecord.fromAddress.Equals(fromAddress)) || !(stakingRecord.assetId.Equals(assetId)))
            {
                return false;
            }
            //Nep5转账
            object[] Params = new object[]
            {
                ExecutionEngine.ExecutingScriptHash,
                fromAddress,
                amount
            };
            DyncCall nep5Contract = (DyncCall)assetId.ToDelegate();
            if (!(bool)nep5Contract("transfer", Params)) throw new Exception(); //throw exception when release

            else             
            {
                BigInteger remainAmount = (stakingRecord.amount - amount);
                UpdateStackRecord(assetId, currentTimeStamp);
                //收益结算
                BigInteger currentProfit = SettleProfit(stakingRecord.timeStamp, stakingRecord.amount, assetId) + stakingRecord.Profit;
                SaveUserStaking(fromAddress, remainAmount, assetId, currentTimeStamp, currentProfit, key);
            }
            return true;
        }

#if DEBUG
        [DisplayName("claimFLM")]
        public static bool ClaimFLM(byte[] fromAddress, byte[] assetId) => true;
#endif
        private static bool ClaimFLM(byte[] fromAddress, byte[] assetId, byte[] callingScript)
        {
            if (IsPaused()) return false;
            if (!Runtime.CheckWitness(fromAddress)) return false;
            var currentTimeStamp = GetCurrentTimeStamp();
            if (!CheckIfRefundStart(currentTimeStamp)) return false;
            byte[] key = assetId.Concat(fromAddress);
            var result = Storage.Get(key);
            if (result.Length == 0) return false;
            StakingReocrd stakingReocrd = (StakingReocrd)Storage.Get(key).Deserialize();
            if (!stakingReocrd.fromAddress.Equals(fromAddress))
            {
                return false;
            }
            UpdateStackRecord(assetId, currentTimeStamp);
            BigInteger newProfit = SettleProfit(stakingReocrd.timeStamp, stakingReocrd.amount, assetId);
            var profitAmount = stakingReocrd.Profit + newProfit;
            if (profitAmount == 0) return true;
            SaveUserStaking(fromAddress, stakingReocrd.amount, stakingReocrd.assetId, currentTimeStamp, 0, key);
            if (!MintFLM(fromAddress, profitAmount, callingScript))
            {
                throw new Exception();
            }
            return true;
        }

        [DisplayName("checkFLM")]
        public static BigInteger CheckFLM(byte[] fromAddress, byte[] assetId) 
        {
            byte[] key = assetId.Concat(fromAddress);
            var result = Storage.Get(key);
            if (result.Length != 0) 
            {
                StakingReocrd stakingReocrd = (StakingReocrd)result.Deserialize();
                UpdateStackRecord(assetId, GetCurrentTimeStamp());
                BigInteger newProfit = SettleProfit(stakingReocrd.timeStamp, stakingReocrd.amount, assetId);
                var profitAmount = stakingReocrd.Profit + newProfit;
                return profitAmount;
            }
            return 0;
        }

        [DisplayName("getStakingAmount")]
        public static BigInteger GetStakingAmount(byte[] fromAddress, byte[] assetId)
        {
            byte[] key = assetId.Concat(fromAddress);
            var result = Storage.Get(key);

            if (result.Length != 0)
            {
                StakingReocrd stakingRecord = (StakingReocrd)result.Deserialize();
                return stakingRecord.amount;
            }
            return 0;
        }

        private static BigInteger SettleProfit(BigInteger recordTimeStamp, BigInteger amount, byte[] assetId) 
        {
            BigInteger MinusProfit = GetHistoryUintStackProfitSum(assetId, recordTimeStamp);
            BigInteger SumProfit = GetHistoryUintStackProfitSum(assetId, GetCurrentTimeStamp());
            BigInteger currentProfit = (SumProfit - MinusProfit) * amount;
            return currentProfit;
        }

        private static bool CheckIfStakingStart(BigInteger currentTimeStamp) 
        {
            if (currentTimeStamp >= StartStakingTimeStamp)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        private static bool CheckIfRefundStart(BigInteger currentTimeStamp) 
        {
            if (currentTimeStamp >= StartClaimTimeStamp)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        private static bool CheckWhetherSelf(byte[] fromAddress) 
        {
            if (fromAddress.Equals(ExecutionEngine.ExecutingScriptHash)) return true;
            return false;
        }
    }
}
