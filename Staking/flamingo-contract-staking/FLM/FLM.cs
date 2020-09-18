using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

[assembly: Features(ContractPropertyState.HasStorage)]

namespace flamingo_contract_staking
{
    public class FLM : SmartContract
    {
        // TODO: replace Pika with authorized account address
        private static readonly byte[] Pika = "AQzRMe3zyGS8W177xLJfewRRQZY2kddMun".ToScriptHash();
        private static readonly byte[] SupplyKey = "sk".AsByteArray();
        private static readonly byte[] PikaCountKey = "pck".AsByteArray();

        private static readonly byte[] BalancePrefix = new byte[] { 0x01, 0x01 };
        private static readonly byte[] AllowancePrefix = new byte[] { 0x01, 0x02 };
        private static readonly byte[] PikaPrefix = new byte[] { 0x01, 0x03 };

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> TransferEvent;
        [DisplayName("approval")]
        public static event Action<byte[], byte[], BigInteger> ApproveEvent;
        [DisplayName("addPika")]
        public static event Action<byte[]> AddPikaEvent;
        [DisplayName("removePika")]
        public static event Action<byte[]> RemovePikaEvent;



        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return false;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                byte[] callingScript = ExecutionEngine.CallingScriptHash;
                if (method == "name") return Name();
                if (method == "symbol") return Symbol();
                if (method == "supportedStandards") return SupportedStandards();
                if (method == "decimals") return Decimals();
                if (method == "totalSupply") return TotalSupply();
                if (method == "balanceOf") return BalanceOf((byte[])args[0]);
                if (method == "allowance") return Allowance((byte[])args[0], (byte[])args[1]);
                if (method == "transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callingScript);
                if (method == "approve") return Approve((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callingScript);
                if (method == "transferFrom") return TransferFrom((byte[])args[0], (byte[])args[1], (byte[])args[2], (BigInteger)args[3]);
                if (method == "addPika") return AddPika((byte[])args[0]);
                if (method == "removePika") return RemovePika((byte[])args[0]);
                if (method == "isPika") return IsPika((byte[])args[0]);
                if (method == "pikaCount") return PikaCount();
                if (method == "mint") return Mint((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callingScript);
            }
            throw new InvalidOperationException("Invalid method: ".AsByteArray().Concat(method.AsByteArray()).AsString());
        }

        [DisplayName("name")]
        public static string Name() => "Flamingo";

        [DisplayName("symbol")]
        public static string Symbol() => "FLM";

        [DisplayName("supportedStandards")]
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            return Storage.Get(SupplyKey).AsBigInteger();
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] owner)
        {
            Assert(owner.Length == 20, "balanceOf: invalid owner-".AsByteArray().Concat(owner).AsString());
            return Storage.Get(BalancePrefix.Concat(owner)).AsBigInteger();
        }

        [DisplayName("allowance")]
        public static BigInteger Allowance(byte[] owner, byte[] spender)
        {
            Assert(owner.Length == 20, "allowance: invalid owner-".AsByteArray().Concat(owner).AsString());
            Assert(spender.Length == 20, "allowance: invalid spender-".AsByteArray().Concat(spender).AsString());
            return Storage.Get(AllowancePrefix.Concat(owner).Concat(spender)).AsBigInteger();
        }
#if DEBUG
        [DisplayName("transfer")] //Only for ABI file
        public static bool Transfer(byte[] from, byte[] to, BigInteger amount) => true;
#endif
        public static bool Transfer(byte[] from, byte[] to, BigInteger amt, byte[] callingScript)
        {           
            Assert(from.Length == 20 && to.Length == 20 , "transfer: invalid from or to, from-".AsByteArray().Concat(from).Concat(" and to-".AsByteArray()).Concat(to).AsString());
            Assert(Runtime.CheckWitness(from) || from.Equals(callingScript), "transfer: CheckWitness failed, from-".AsByteArray().Concat(from).AsString());
            Assert(amt >= 0, "transfer: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());

            if (from.Equals(to))
            {
                TransferEvent(from, to, amt);
                return true;
            }
            BigInteger fromAmt = Storage.Get(BalancePrefix.Concat(from)).AsBigInteger();

            if (fromAmt < amt)
            {
                return false;
            }
            if (amt > 0)
            {
                if (fromAmt == amt)
                    Storage.Delete(BalancePrefix.Concat(from));
                else
                    Storage.Put(BalancePrefix.Concat(from), fromAmt - amt);

                BigInteger toAmt = Storage.Get(BalancePrefix.Concat(to)).AsBigInteger();
                Storage.Put(BalancePrefix.Concat(to), toAmt + amt);
            }
            TransferEvent(from, to, amt);

            return true;
        }


        [DisplayName("approve")]
        public static bool Approve(byte[] owner, byte[] spender, BigInteger amt, byte[] callingScript)
        {
            Assert(owner.Length == 20 && spender.Length == 20, "approve: invalid owner or spender, owner-".AsByteArray().Concat(owner).Concat("and spender-".AsByteArray()).Concat(spender).AsString());
            Assert(amt > 0, "approve: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());
            Assert(Runtime.CheckWitness(owner) || owner.Equals(callingScript), "approve: CheckWitness failed, owner-".AsByteArray().Concat(owner).AsString());
            if (spender.Equals(owner)) return true;
            Storage.Put(AllowancePrefix.Concat(owner).Concat(spender), amt);
            ApproveEvent(owner, spender, amt);
            return true;
        }

        [DisplayName("transferFrom")]
        public static bool TransferFrom(byte[] spender, byte[] owner, byte[] receiver, BigInteger amt)
        {
            Assert(spender.Length == 20 && owner.Length == 20 && receiver.Length == 20, "transferFrom: invalid spender or owner or receiver, spender-".AsByteArray().Concat(spender).Concat(", owner-".AsByteArray()).Concat(owner).Concat(" and receiver-".AsByteArray()).Concat(receiver).AsString());
            Assert(amt >= 0, "transferFrom: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());
            Assert(Runtime.CheckWitness(spender), "transferFrom: CheckWitness failed, spender-".AsByteArray().Concat(spender).AsString());            
            if (spender.Equals(owner) || owner.Equals(receiver))
            {
                return true;
            }
            if (amt == 0) 
            {
                TransferEvent(owner, receiver, amt);
                return true; 
            }
            byte[] ownerKey = BalancePrefix.Concat(owner);
            BigInteger ownerAmt = Storage.Get(ownerKey).AsBigInteger();
            Assert(ownerAmt >= amt, "transferFrom: Owner balance-".AsByteArray().Concat(amt.ToByteArray()).Concat(" is less than amt-".AsByteArray()).Concat(amt.ToByteArray()).AsString());

            byte[] allowanceKey = AllowancePrefix.Concat(owner).Concat(spender);
            BigInteger allowance = Storage.Get(allowanceKey).AsBigInteger();

            Assert(allowance >= amt, "transferFrom: allowance-".AsByteArray().Concat(allowance.ToByteArray()).Concat(" is less than amt-".AsByteArray()).Concat(amt.ToByteArray()).AsString());

            if (amt == allowance)
            {
                Storage.Delete(allowanceKey);
            }
            else
            {
                Storage.Put(allowanceKey, allowance - amt);
            }

            if (amt == ownerAmt)
            {
                Storage.Delete(ownerKey);
            }
            else
            {
                Storage.Put(ownerKey, ownerAmt - amt);
            }

            Storage.Put(BalancePrefix.Concat(receiver), Storage.Get(BalancePrefix.Concat(receiver)).AsBigInteger() + amt);

            TransferEvent(owner, receiver, amt);

            return true;
        }

        [DisplayName("mint")]
        public static bool Mint(byte[] pika, byte[] receiver, BigInteger amt, byte[] callingScript)
        {
            amt = amt / 100000000000000;
            Assert(pika.Length == 20 && receiver.Length == 20, "mint: invalid pika or receiver, pika-".AsByteArray().Concat(pika).Concat(" and receiver-".AsByteArray()).Concat(receiver).AsString());
            Assert(amt > 0, "mint: invalid amount-".AsByteArray().Concat(amt.ToByteArray()).AsString());

            Assert(IsPika(pika) || pika.Equals(Pika), "mint: pika-".AsByteArray().Concat(pika).Concat(" is not a real pika".AsByteArray()).AsString());
            Assert(Runtime.CheckWitness(pika) || pika.Equals(callingScript), "mint: CheckWitness failed, pika-".AsByteArray().Concat(pika).AsString());

            Storage.Put(BalancePrefix.Concat(receiver), BalanceOf(receiver) + amt);
            Storage.Put(SupplyKey, TotalSupply() + amt);
            TransferEvent(null, receiver, amt);
            return true;
        }


        [DisplayName("addPika")]
        public static bool AddPika(byte[] newPika)
        {
            Assert(Runtime.CheckWitness(Pika) && !newPika.Equals(Pika), "addPika: CheckWitness failed, only first pika can add other pika");
            Assert(!IsPika(newPika), "addPika: newPika-".AsByteArray().Concat(newPika).Concat(" is already a pika".AsByteArray()).AsString());
            Storage.Put(PikaPrefix.Concat(newPika), 1);
            Storage.Put(PikaCountKey, Storage.Get(PikaCountKey).AsBigInteger() + 1);
            AddPikaEvent(newPika);
            return true;
        }

        [DisplayName("removePika")]
        public static bool RemovePika(byte[] pika)
        {
            Assert(Runtime.CheckWitness(Pika) && !pika.Equals(Pika), "removePika: CheckWitness failed, only first pika can remove other pika");
            Assert(IsPika(pika), "removePika: pika-".AsByteArray().Concat(pika).Concat(" is NOT a pika".AsByteArray()).AsString());
            Storage.Delete(PikaPrefix.Concat(pika));
            Storage.Put(PikaCountKey, Storage.Get(PikaCountKey).AsBigInteger() - 1);
            RemovePikaEvent(pika);
            return true;
        }

        [DisplayName("isPika")]
        public static bool IsPika(byte[] pika)
        {
            return Storage.Get(PikaPrefix.Concat(pika)).Length != 0 || pika.Equals(Pika);
        }

        [DisplayName("pikaCount")]
        public static BigInteger PikaCount()
        {
            return Storage.Get(PikaCountKey).AsBigInteger() + 1;
        }


        private static void Assert(bool condition, string msg)
        {
            if (!condition)
            {
                // TODO: uncomment next line on mainnet
                //throw new InvalidOperationException("transfer: from equals to address.");
                Runtime.Notify(Symbol().AsByteArray().Concat(msg.AsByteArray()).AsString());
            }
        }

    }
}
