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
        public static object Main(string method, object[] args)
        {            
            return false;
        }

        public static byte[] Staking(byte[] fromAddress, BigInteger amount, byte[] assetId) 
        {
            byte[] txHash = ((Transaction)ExecutionEngine.ScriptContainer).Hash;
            //TODO: Nep5转账操作
            BigInteger currentHeight = Blockchain.GetHeight();
            SaveUserStaking(fromAddress, amount, assetId, currentHeight, 0, txHash);
            UpdateStackRecord(amount, assetId);
            return txHash;
        }

        public static bool Refund(byte[] fromAddress, BigInteger amount, byte[] assetId, byte[] keyHash) 
        {
            Runtime.CheckWitness(fromAddress);
            BigInteger currentHeight = Blockchain.GetHeight();
            //提现检查
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
            return true;
        }

        private static void UpdateStackRecord(BigInteger amount, byte[] assetId) 
        {
            //清算历史每stack收益率
            UpdateHistoryUintStackProfitSum(assetId);
            //更新当前收益率记账高度    
            UpdateCurrentRecordHeight(assetId);
            //计算之后每stack收益率
            var currentTotalStakingAmount = SaveTotalAmountIncrease(assetId, amount);
            var currentShareAmount = GetCurrentShareAmount();
            //TODO: 做正负号检查
            var currentUintStackProfit = currentShareAmount / currentTotalStakingAmount;
            //更新当前每个stack收益率
            UpdateCurrentUintStackProfit(assetId, currentUintStackProfit);
        }
      
        public static BigInteger GetCurrentIndex(byte[] assetId)
        {
            return Storage.Get("Index".AsByteArray().Concat(assetId)).ToBigInteger();
        }

        private static BigInteger GetCurrentShareAmount() 
        {
            //获取最新高度块上的分红总量
            return 1;
        }

        private static BigInteger GetCurrentTotalAmount(byte[] assetId) 
        {
            return Storage.Get(_currentTotalAmount.Concat(assetId)).ToBigInteger();
        }

        private static BigInteger SaveTotalAmountIncrease(byte[] assetId, BigInteger amount) 
        {
            var totalAmount = GetCurrentTotalAmount(assetId) + amount;
            Storage.Put(_currentTotalAmount.Concat(assetId), totalAmount);
            return totalAmount;
        }




    }
}
