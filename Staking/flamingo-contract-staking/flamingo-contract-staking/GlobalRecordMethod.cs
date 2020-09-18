using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] _historyUintStackProfitSum = new byte[] { 0x02, 0x01 };
        private static BigInteger GetHistoryUintStackProfitSum(byte[] assetId, BigInteger TimeStamp)
        {
            byte[] key = _historyUintStackProfitSum.Concat(assetId.Concat(TimeStamp.AsByteArray()));
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

        private static BigInteger GetCurrentTimeStamp() 
        {
            return Runtime.Time;
        }

        private static void UpdateStackRecord(byte[] assetId, BigInteger currentTimeStamp)
        {
            //清算历史每stack收益率
            UpdateHistoryUintStackProfitSum(assetId, currentTimeStamp);
            //更新当前收益率记账高度    
            UpdateCurrentRecordTimeStamp(assetId);
            //计算之后每stack收益率
            var currentTotalStakingAmount = GetCurrentTotalAmount(assetId);
            var currentShareAmount = GetCurrentShareAmount(assetId);
            BigInteger currentUintStackProfit = 0;
            //TODO: 做正负号检查
            if (currentTotalStakingAmount != 0)
            {
                currentUintStackProfit =  currentShareAmount / currentTotalStakingAmount;
            }          
            //更新当前每个stack收益率
            UpdateCurrentUintStackProfit(assetId, currentUintStackProfit);
        }

        private static void UpdateHistoryUintStackProfitSum(byte[] assetId, BigInteger currentTimeStamp)
        {            
            BigInteger recordTimeStamp = GetCurrentRecordTimeStamp(assetId);
            if (recordTimeStamp >= currentTimeStamp)
            {
                return;
            }
            else
            {
                var uintStackProfit = GetCurrentUintStackProfit(assetId);
                BigInteger increaseAmount = uintStackProfit * (currentTimeStamp - recordTimeStamp);
                byte[] key = _historyUintStackProfitSum.Concat(assetId.Concat(currentTimeStamp.AsByteArray()));
                Storage.Put(key, increaseAmount + GetHistoryUintStackProfitSum(assetId, recordTimeStamp));
            }
        }

        private static BigInteger GetCurrentRecordTimeStamp(byte[] assetId)
        {
            byte[] currentRateTimeStampKey = _currentRateTimeStampPrefix.Concat(assetId);
            var result = Storage.Get(currentRateTimeStampKey);
            if (result.Length == 0)
            {
                return StartStakingTimeStamp;
            }
            else
            {
                return result.ToBigInteger();
            }
        }

        private static void UpdateCurrentRecordTimeStamp(byte[] assetId)
        {
            byte[] currentRateTimeStampKey = _currentRateTimeStampPrefix.Concat(assetId);
            Storage.Put(currentRateTimeStampKey, GetCurrentTimeStamp());
        }

        private static BigInteger GetCurrentUintStackProfit(byte[] assetId)
        {
            byte[] _UintStackProfitKey = _currentUintStackProfitPrefix.Concat(assetId);
            if (_UintStackProfitKey.Length == 0) return 0;
            return Storage.Get(_UintStackProfitKey).ToBigInteger();
        }

        private static void UpdateCurrentUintStackProfit(byte[] assetId, BigInteger profit)
        {
            byte[] _UintStackProfitKey = _currentUintStackProfitPrefix.Concat(assetId);
            Storage.Put(_UintStackProfitKey, profit);
        }

        [DisplayName("getCurrentTotalAmount")]
        public static BigInteger GetCurrentTotalAmount(byte[] assetId)
        {
            var Params = new object[] { ExecutionEngine.ExecutingScriptHash };
            BigInteger totalAmount = (BigInteger)((DyncCall)assetId.ToDelegate())("balanceOf", Params);
            return totalAmount;
        }
    }
}