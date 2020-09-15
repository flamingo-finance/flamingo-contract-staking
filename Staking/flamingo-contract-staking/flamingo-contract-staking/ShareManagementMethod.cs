using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;

namespace flamingo_contract_staking
{
    public partial class StakingContract : SmartContract
    {
        private static readonly byte[] _currentShareAmount = new byte[] { 0x06, 0x01 };

        [DisplayName("setshareamount")]
        public static bool SetCurrentShareAmount(byte[] assetId, BigInteger amount, byte[] adminAddress) 
        {
            if (IsInWhiteList(assetId) && IsAdmin(adminAddress) && Runtime.CheckWitness(adminAddress))
            {
                if (amount >= 0)
                {
                    byte[] key = _currentShareAmount.Concat(assetId);
                    Storage.Put(key, amount);                    
                    UpdateStackRecord(assetId, GetCurrentTimeStamp());
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else 
            {
                return false;
            }
        }

        [DisplayName("getshareamount")]
        public static BigInteger GetCurrentShareAmount(byte[] assetId) 
        {
            if (IsInWhiteList(assetId)) 
            {
                byte[] key = _currentShareAmount.Concat(assetId);
                var result = Storage.Get(key);
                if (result.Length != 0)
                {
                    return result.ToBigInteger();
                }
            }
            return 0;
        }
    }
}
