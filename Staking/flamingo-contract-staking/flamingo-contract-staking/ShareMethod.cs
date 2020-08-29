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
        public static BigInteger GetCurrentShareAmount(byte[] assetId)
        {
            //获取最新高度块上的分红总量
            return 1;
        }
    }
}
