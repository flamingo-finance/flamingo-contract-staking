﻿using System.Numerics;
using Neo.SmartContract.Framework;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        struct StakingReocrd
        {
            public BigInteger timeStamp;
            public byte[] fromAddress;
            public BigInteger amount;
            public byte[] assetId;
            public BigInteger Profit;
        }
    }
}
