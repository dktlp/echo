using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Echo.Runtime.Engine.Cryptography
{
    public static class RSAParametersHelper
    {
        public static RSAParameters FromBytes(byte[] bytes)
        {
            RSAParameters rsap = new RSAParameters()
            {
                Exponent = new byte[3],
                Modulus = new byte[bytes.Length - 3]
            };

            Array.Copy(bytes, rsap.Exponent, rsap.Exponent.Length);
            Array.Copy(bytes, rsap.Exponent.Length, rsap.Modulus, 0, rsap.Modulus.Length);

            return rsap;
        }

        public static RSAParameters FromBase64(string base64)
        {
            return FromBytes(Convert.FromBase64String(base64));
        }

        public static string ToBase64(RSAParameters rsap)
        {
            // Only export the private key.
            // Exponent (3 bytes) / Modulus (256 bytes) <- Base64 encode

            byte[] bytes = new byte[rsap.Exponent.Length + rsap.Modulus.Length];

            Array.Copy(rsap.Exponent, bytes, rsap.Exponent.Length);
            Array.Copy(rsap.Modulus, 0, bytes, rsap.Exponent.Length, rsap.Modulus.Length);

            return Convert.ToBase64String(bytes);
        }
    }
}