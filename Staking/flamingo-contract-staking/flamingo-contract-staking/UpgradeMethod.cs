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
        public static bool Upgrade(byte[] newScript, byte[] paramList, byte returnType, ContractPropertyState cps, string name, string version, string author, string email, string description)
        {
            if (!Runtime.CheckWitness(originOwner)) return false;
            byte[] newContractHash = Hash160(newScript);
            if (!TransferAssetsToNewContract(newContractHash)) throw new Exception();
            Contract.Migrate(newScript, paramList, returnType, cps, name, version, author, email, description);
            Runtime.Notify(new object[] { "upgrade", ExecutionEngine.ExecutingScriptHash, newContractHash });
            return true;
        }

        private static bool TransferAssetsToNewContract(byte[] newContractHash)
        {
            // Try to transfer nep5 asset from old contract into new contract
            byte[] assetHashMapInfo = Storage.Get(assetMapPrefix);
            if (assetHashMapInfo.Length > 0)
            {
                Map<byte[], bool> assetHashMap = (Map<byte[], bool>)assetHashMapInfo.Deserialize();
                byte[][] assetHashes = assetHashMap.Keys;

                byte[] self = ExecutionEngine.ExecutingScriptHash;
                BigInteger assetBalance;
                bool success;
                foreach (var assetHash in assetHashes)
                {
                    if (assetHashMap[assetHash] && assetHash.Length == 20)
                    {
                        assetBalance = (BigInteger)((DyncCall)assetHash.ToDelegate())("balanceOf", new object[] { self });
                        if (assetBalance > 0)
                        {
                            success = (bool)((DyncCall)assetHash.ToDelegate())("transfer", new Object[] { self, newContractHash, assetBalance });
                        }
                    }
                }
            }
            return true;
        }
    }
}
