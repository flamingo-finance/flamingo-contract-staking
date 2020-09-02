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
        private static readonly byte[] _flmPrefix = new byte[] { 0x07, 0x01 };
        private static bool MintFLM(byte[] receiver, BigInteger amount, byte[] callingScript)
        {
            var Params = new object[]
            {
                callingScript,
                receiver,
                amount
            };
            byte[] flmHash = GetFlmAddress();
            if (flmHash.Length == 0)
            {
                Runtime.Notify("flmHash not set");
                return false;
                //throw new Exception();
            }
            else 
            {
                return (bool)((DyncCall)(flmHash.ToDelegate()))("Mint", Params);
            }
        }

        public static bool SetFlmAddress(byte[] flmScriptHash, byte[] adminHash)
        {
            if (Runtime.CheckWitness(adminHash) && IsAdmin(adminHash) && flmScriptHash.Length == 20)
            {
                Storage.Put(_flmPrefix, flmScriptHash);
                return true;
            }
            else 
            {
                return false;
            }
        }

        public static byte[] GetFlmAddress()       
        {
            return Storage.Get(_flmPrefix);
        }
    }
}
