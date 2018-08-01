using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Neo;

namespace ConsoleApp1
{
    static class Program
    {
        static void Main(string[] args)
        {
            //address 2 script hash
            Console.WriteLine(Neo.Wallets.Wallet.ToScriptHash("AJY2kq7MmJP15Qv4WRiBUF7FtXCw3JbAt5"));
            //script hash 2 address
            Console.WriteLine(Neo.Wallets.Wallet.ToAddress(new UInt160("0xeee76c45175b5af08630b2278a24803368284a1e".Remove(0, 2).HexToBytes().Reverse().ToArray())));            

            //hex string 2 string
            Console.WriteLine("7472616e73666572".HexToString());
            //string 2 hex string
            Console.WriteLine("transfer".ToHexString());

            //big-endian 2 little-endian
            Console.WriteLine("0x2335efb64138479aea93a36cacd8be670acf47733b019bd118b9e793404aadb1".Remove(0, 2).HexToBytes().Reverse().ToHexString());
            //little-endian 2 big-endian
            Console.WriteLine("0x" + "b1ad4a4093e7b918d19b013b7347cf0a67bed8ac6ca393ea9a473841b6ef3523".HexToBytes().Reverse().ToHexString());

            //hex string 2 biginteger
            Console.WriteLine(new BigInteger("00e1f505".HexToBytes()));
            //biginteger 2 hex string
            Console.WriteLine(new BigInteger(100000000).ToByteArray().ToHexString());

            Console.ReadLine();
        }

        static string HexToString(this string hex)
        {
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException();
            }
            var output = "";
            for (int i = 0; i <= hex.Length - 2; i+=2)
            {
                try
                {
                    var result = Convert.ToByte(new string(hex.Skip(i).Take(2).ToArray()), 16);
                    output += (Convert.ToChar(result));
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return output;
        }
        static string ToHexString(this string str)
        {
            byte[] byteArray = Encoding.Default.GetBytes(str.ToCharArray());
            return byteArray.ToHexString();
        }
    }
}
