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
        private static byte[] _currentTotalAmount = new byte[] { 0x00, 0x01 };
        private static byte[] _currentRateHeightPrefix = new byte[] { 0x01, 0x01 };        
        private static byte[] _currentUintStackProfitPrefix = new byte[] { 0x01, 0x02 };

        private static byte[] _historyUintStackProfitSum = new byte[] { 0x02, 0x01 };

        private static BigInteger StartHeight = 10000;
        delegate object DyncCall(string method, object[] args);
        public static object Main(string method, object[] args)
        {
            return false;
        }

        public static byte[] Staking(byte[] fromAddress, BigInteger amount, byte[] assetId) 
        {
            if (!IsInWhiteList(assetId) || assetId.Length != 20) return new byte[] { 0x00 }; //throw exception when release
            DyncCall nep5Contract = (DyncCall)assetId.ToDelegate();
            byte[] txHash = ((Transaction)ExecutionEngine.ScriptContainer).Hash;
            object[] Params = new object[]
            {
                fromAddress,
                ExecutionEngine.ExecutingScriptHash,
                amount
            };
            if (!(bool)nep5Contract("transfer", Params)) return new byte[] { 0x01 }; //throw exception when release
            BigInteger currentHeight = Blockchain.GetHeight();
            SaveUserStaking(fromAddress, amount, assetId, currentHeight, 0, txHash);
            UpdateStackRecord(amount, assetId);
            return txHash;
        }

        public static bool Refund(byte[] fromAddress, BigInteger amount, byte[] assetId, byte[] keyHash) 
        {
            //提现检查
            if (!Runtime.CheckWitness(fromAddress)) return false;
            BigInteger currentHeight = Blockchain.GetHeight();
            StakingReocrd stakingReocrd = (StakingReocrd)Storage.Get(keyHash).Deserialize();
            if (stakingReocrd.amount < amount || !(stakingReocrd.fromAddress.Equals(fromAddress)) || !(stakingReocrd.assetId.Equals(assetId)))
            {
                return false;
            }
            else             
            {
                BigInteger remainAmount = (stakingReocrd.amount - amount);
                UpdateStackRecord( -remainAmount, assetId);
                //计算需要扣除的收益
                BigInteger MinusProfit = GetHistoryUintStackProfitSum(assetId, stakingReocrd.height);
                //计算总收益
                BigInteger SumProfit = GetHistoryUintStackProfitSum(assetId, currentHeight);
                SaveUserStaking(fromAddress, remainAmount, assetId, currentHeight, SumProfit - MinusProfit, keyHash);
            }
            //Nep5转账
            DyncCall nep5Contract = (DyncCall)assetId.ToDelegate();
            object[] Params = new object[]
            {                
                ExecutionEngine.ExecutingScriptHash,
                fromAddress,
                amount
            };
            if (!(bool)nep5Contract("transfer", Params)) return false; //throw exception when release
            return true;
        }

        public static bool ClaimFLM(byte[] fromAddress, byte[] keyHash, byte[] callingScript) 
        {
            if (!Runtime.CheckWitness(fromAddress)) return false;
            StakingReocrd stakingReocrd = (StakingReocrd)Storage.Get(keyHash).Deserialize();
            if (stakingReocrd.fromAddress.Equals(fromAddress))
            {
                return false;
            }
            var profitAmount = stakingReocrd.Profit;
            SaveUserStaking(fromAddress, stakingReocrd.amount, stakingReocrd.assetId, stakingReocrd.height, 0, keyHash);
            if (!MintFLM(fromAddress, profitAmount, callingScript))             
            {
                return false;
            }
            return true;
        }
    }
}
