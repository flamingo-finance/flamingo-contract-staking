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
        private static void SaveUserStaking(byte[] fromAddress, BigInteger amount, byte[] assetId, BigInteger timeStamp, BigInteger profit, byte[] key)
        {
            StakingReocrd record = new StakingReocrd
            {
                timeStamp = timeStamp,
                fromAddress = fromAddress,
                amount = amount,
                assetId = assetId,
                Profit = profit
            };
            Storage.Put(key, record.Serialize());
        }
    }
}
