using System;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.System;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] upgradeTimelockPrefix = new byte[] { 0x08, 0x01 };
        [DisplayName("upgradeStart")]
        public static bool UpgradeStart() 
        {
            if (!Runtime.CheckWitness(GetOwner())) return false;
            var upgradeTimelock = Storage.Get(upgradeTimelockPrefix);
            if (upgradeTimelock.Length != 0) return false;
            Storage.Put(upgradeTimelockPrefix, GetCurrentTimeStamp() + 86400);
            return true;
        }

        [DisplayName("upgrade")]
        public static bool Upgrade(byte[] newScript, byte[] paramList, byte returnType, int cps, string name, string version, string author, string email, string description)
        {
            if (!Runtime.CheckWitness(GetOwner())) return false;
            byte[] newContractHash = Hash160(newScript);
            if (!Blockchain.GetContract(newContractHash).Serialize().Equals(new byte[] { 0x00, 0x00 })) throw new Exception();
            if (!UpgradeEnd()) return false;
            if (!TransferAssetsToNewContract(newContractHash)) throw new Exception();
            Contract.Migrate(newScript, paramList, returnType, (ContractPropertyState)cps, name, version, author, email, description);
            Runtime.Notify(new object[] { "upgrade", ExecutionEngine.ExecutingScriptHash, newContractHash });
            return true;
        }

        private static bool UpgradeEnd() 
        {
            var upgradeTimelock = Storage.Get(upgradeTimelockPrefix);
            if (upgradeTimelock.Length == 0) return false;
            if (upgradeTimelock.ToBigInteger() > GetCurrentTimeStamp()) return false;
            Storage.Put(upgradeTimelockPrefix, new byte[0]);
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
                            Runtime.Notify(assetHash, success);
                        }
                    }
                }
            }
            return true;
        }
    }
}
