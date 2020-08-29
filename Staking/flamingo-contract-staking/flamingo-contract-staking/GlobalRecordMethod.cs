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
        private static BigInteger GetHistoryUintStackProfitSum(byte[] assetId, BigInteger height)
        {
            byte[] key = _historyUintStackProfitSum.Concat(assetId.Concat(height.AsByteArray()));
            var result = Storage.Get(key);
            if (result.Length == 0)
            {
                return 0;
            }
            else
            {
                return result.ToBigInteger();
            }
        }

        private static void UpdateStackRecord(BigInteger amount, byte[] assetId)
        {
            //清算历史每stack收益率
            UpdateHistoryUintStackProfitSum(assetId);
            //更新当前收益率记账高度    
            UpdateCurrentRecordHeight(assetId);
            //计算之后每stack收益率
            var currentTotalStakingAmount = SaveTotalAmountIncrease(assetId, amount);
            var currentShareAmount = GetCurrentShareAmount(assetId);
            //TODO: 做正负号检查
            var currentUintStackProfit = currentShareAmount / currentTotalStakingAmount;
            //更新当前每个stack收益率
            UpdateCurrentUintStackProfit(assetId, currentUintStackProfit);
        }

        private static bool UpdateHistoryUintStackProfitSum(byte[] assetId)
        {
            BigInteger currentHeight = Blockchain.GetHeight();
            BigInteger recordHeight = GetCurrentRecordHeight(assetId);
            if (recordHeight >= currentHeight)
            {
                return false;
            }
            else
            {
                var uintStackProfit = GetCurrentUintStackProfit(assetId);
                BigInteger increaseAmount = uintStackProfit * (currentHeight - recordHeight);
                byte[] key = _historyUintStackProfitSum.Concat(assetId.Concat(currentHeight.AsByteArray()));
                Storage.Put(key, increaseAmount + GetHistoryUintStackProfitSum(assetId, recordHeight));
                return true;
            }
        }

        private static BigInteger GetCurrentRecordHeight(byte[] assetId)
        {
            byte[] currentRateHeightKey = _currentRateHeightPrefix.Concat(assetId);
            var result = Storage.Get(currentRateHeightKey);
            if (result.Length == 0)
            {
                return StartHeight;
            }
            else
            {
                return result.ToBigInteger();
            }
        }

        private static bool UpdateCurrentRecordHeight(byte[] assetId)
        {
            byte[] currentRateHeightKey = _currentRateHeightPrefix.Concat(assetId);
            BigInteger currentHeight = Blockchain.GetHeight();
            Storage.Put(currentRateHeightKey, currentHeight);
            return true;
        }

        private static BigInteger GetCurrentUintStackProfit(byte[] assetId)
        {
            byte[] _UintStackProfitKey = _currentUintStackProfitPrefix.Concat(assetId);
            return Storage.Get(_UintStackProfitKey).ToBigInteger();
        }

        private static bool UpdateCurrentUintStackProfit(byte[] assetId, BigInteger profit)
        {
            byte[] _UintStackProfitKey = _currentUintStackProfitPrefix.Concat(assetId);
            Storage.Put(_UintStackProfitKey, profit);
            return true;
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