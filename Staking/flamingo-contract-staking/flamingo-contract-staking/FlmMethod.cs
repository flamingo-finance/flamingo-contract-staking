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
        private static bool MintFLM(byte[] receiver, BigInteger amount, byte[] callingScript)
        {
            var Params = new object[]
            {
                callingScript,
                receiver,
                amount
            };
            return FlmMain("mint", Params);
        }

        //FLM合约
        [Appcall("b9d7ea3062e6aeeb3e8ad9548220c4ba1361d263")]
        public static extern bool FlmMain(string operation, params object[] args);
    }
}
