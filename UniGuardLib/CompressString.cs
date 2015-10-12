using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace UniGuardLib
{
    public class CompressString
    {

        //Only strings, no byte arrays are held within the class instance.
        //That avoids unnecessary conversions when requesting data more than
        //once from the class instance, without giving any new input.
        private string _UnCompressed = string.Empty;
        private string _Compressed = string.Empty;
        private System.Text.Encoding _TextEncoding = System.Text.Encoding.UTF8;
        private string _PrefixForCompressedString = string.Empty;
        private string _SuffixForCompressedString = string.Empty;
        private string _Passphrase = string.Empty;

        private bool _CompressedGiven = false;
        public enum InputDataTypeClass
        {
            Compressed = 1,
            UnCompressed = 2
        }

        public string UnCompressed
        {
            get { return this._UnCompressed; }
            set
            {
                this._UnCompressed = value;
                //Remember that uncompressed data was initially given
                this._CompressedGiven = false;
                //With the uncompressed data set we can start compression right away
                this.Compress();
            }
        }

        public string Compressed
        {
            get
            {
                //If required, add prefix and/or suffix to the compressed string before returning it
                string Result = this._Compressed;
                if (Result.Length > 0)
                {
                    if (this._PrefixForCompressedString.Length > 0)
                        Result = this._PrefixForCompressedString + Result;
                    if (this._SuffixForCompressedString.Length > 0)
                        Result = Result + this._SuffixForCompressedString;
                }
                return Result;
            }
            set
            {
                //If required, remove prefix and/or suffix from the given compressed string
                string Result = value;
                if (Result.Length > 0 & Result.Length > (this._PrefixForCompressedString.Length + this._SuffixForCompressedString.Length))
                {
                    if (this._PrefixForCompressedString.Length > 0)
                    {
                        Result = value.Substring(this._PrefixForCompressedString.Length, Result.Length - this._PrefixForCompressedString.Length);
                    }
                    if (this._SuffixForCompressedString.Length > 0)
                    {
                        Result = Result.Substring(0, Result.Length - this._SuffixForCompressedString.Length);
                    }
                }
                this._Compressed = Result;
                //Remember that compressed data was initially given
                this._CompressedGiven = true;
                //With the compressed data set we can start decompression right away
                this.Decompress();
            }
        }

        public System.Text.Encoding TextEncoding
        {
            get { return this._TextEncoding; }
            set { this._TextEncoding = value; }
        }

        public long UnCompressed_Size
        {
            get { return this._UnCompressed.Length; }
        }

        public long Compressed_Size
        {
            get { return this._Compressed.Length; }
        }

        public double Compression_Ratio
        {
            get
            {
                double Result = 0;
                if (this._Compressed.Length > 0 & this._UnCompressed.Length > 0)
                {
                    Result = this._Compressed.Length / this._UnCompressed.Length;
                    Result = Math.Round(Result, 2, MidpointRounding.AwayFromZero);
                }
                return Result;
            }
        }

        public string PrefixForCompressedString
        {
            get { return this._PrefixForCompressedString; }
            set { this._PrefixForCompressedString = value; }
        }

        public string SuffixForCompressedString
        {
            get { return this._SuffixForCompressedString; }
            set { this._SuffixForCompressedString = value; }
        }

        public string Passphrase
        {
            get { return this._Passphrase; }
            set
            {
                this._Passphrase = value;
                //With a new passphrase we possibly need to do a de-/compress,
                //if any data is available - depending on the desired direction.
                if (this._CompressedGiven)
                {
                    if (this._Compressed.Length > 0)
                    {
                        this.Decompress();
                    }
                    else
                    {
                        this._UnCompressed = string.Empty;
                    }
                }
                else
                {
                    if (this._UnCompressed.Length > 0)
                    {
                        this.Compress();
                    }
                    else
                    {
                        this._Compressed = string.Empty;
                    }
                }
            }
        }

        //No Parameters given

        public CompressString()
        {
        }

        //Only TextEncoding given

        public CompressString(System.Text.Encoding TextEncoding)
        {
            this._TextEncoding = TextEncoding;

        }

        //Direct Compression/Decompression

        public CompressString(System.Text.Encoding TextEncoding, string InputString, InputDataTypeClass InputDataType, string Passphrase = "", string PrefixForCompressedString = "", string SuffixForCompressedString = "")
        {
            this._TextEncoding = TextEncoding;
            this._PrefixForCompressedString = PrefixForCompressedString;
            this._SuffixForCompressedString = SuffixForCompressedString;
            this._Passphrase = Passphrase;

            switch (InputDataType)
            {
                case InputDataTypeClass.UnCompressed:
                    this._UnCompressed = InputString;
                    this.Compress();
                    break;
                case InputDataTypeClass.Compressed:
                    string Result = InputString;
                    if (Result.Length > 0 & Result.Length > (this._PrefixForCompressedString.Length + this._SuffixForCompressedString.Length))
                    {
                        if (this._PrefixForCompressedString.Length > 0)
                        {
                            Result = InputString.Substring(this._PrefixForCompressedString.Length, Result.Length - this._PrefixForCompressedString.Length);
                        }
                        if (this._SuffixForCompressedString.Length > 0)
                        {
                            Result = Result.Substring(0, Result.Length - this._SuffixForCompressedString.Length);
                        }
                    }
                    this._Compressed = Result;
                    this._CompressedGiven = true;
                    this.Decompress();
                    break;
            }

        }


        private void Compress()
        {
            if (this._UnCompressed.Length == 0)
            {
                this._Compressed = string.Empty;
                return;
            }

            string Result = string.Empty;


            try
            {
                //Convert the uncompressed string into a byte array
                byte[] UnZippedData = this._TextEncoding.GetBytes(this._UnCompressed);

                //Compress the byte array
                System.IO.MemoryStream MS = new System.IO.MemoryStream();
                System.IO.Compression.GZipStream GZip = new System.IO.Compression.GZipStream(MS, System.IO.Compression.CompressionMode.Compress);
                GZip.Write(UnZippedData, 0, UnZippedData.Length);
                //Don't FLUSH here - it possibly leads to data loss!
                GZip.Close();

                byte[] ZippedData = null;

                //Encrypt the compressed byte array, if required
                if (this._Passphrase.Length > 0)
                {
                    ZippedData = Encrypt(MS.ToArray());
                }
                else
                {
                    ZippedData = MS.ToArray();
                }

                //Convert the compressed byte array back to a string
                Result = System.Convert.ToBase64String(ZippedData);

                MS.Close();
                GZip.Dispose();
                MS.Dispose();
            }
            catch (Exception)
            {
                //Keep quiet - in case of an exception an empty string is returned
            }
            finally
            {
                this._Compressed = Result;
            }

        }


        private void Decompress()
        {
            if (this._Compressed.Length == 0)
            {
                this._UnCompressed = string.Empty;
                return;
            }

            string Result = string.Empty;

            try
            {
                byte[] ZippedData = null;

                //Convert the compressed string into a byte array and decrypt the array if required
                if (this._Passphrase.Length > 0)
                {
                    ZippedData = Decrypt(System.Convert.FromBase64String(this._Compressed));
                }
                else
                {
                    ZippedData = System.Convert.FromBase64String(this._Compressed);
                }

                //Decompress the byte array
                System.IO.MemoryStream objMemStream = new System.IO.MemoryStream(ZippedData);
                System.IO.Compression.GZipStream objGZipStream = new System.IO.Compression.GZipStream(objMemStream, System.IO.Compression.CompressionMode.Decompress);
                byte[] sizeBytes = new byte[4];

                objMemStream.Position = objMemStream.Length - 4;
                objMemStream.Read(sizeBytes, 0, 4);

                int iOutputSize = BitConverter.ToInt32(sizeBytes, 0);

                objMemStream.Position = 0;

                byte[] UnZippedData = new byte[iOutputSize];

                objGZipStream.Read(UnZippedData, 0, iOutputSize);

                objGZipStream.Dispose();
                objMemStream.Dispose();

                //Convert the decompressed byte array back to a string
                Result = this._TextEncoding.GetString(UnZippedData);


            }
            catch (Exception)
            {
            }
            finally
            {
                this._UnCompressed = Result;
            }

        }

        private byte[] Encrypt(byte[] PlainData)
        {

            byte[] Result = null;


            try
            {
                System.Security.Cryptography.RijndaelManaged Enc = new System.Security.Cryptography.RijndaelManaged();
                Enc.KeySize = 256;
                Enc.Key = this.Encryption_Key();
                Enc.IV = this.Encryption_IV();

                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
                System.Security.Cryptography.CryptoStream cryptoStream = null;
                cryptoStream = new System.Security.Cryptography.CryptoStream(memoryStream, Enc.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);
                cryptoStream.Write(PlainData, 0, PlainData.Length);
                cryptoStream.FlushFinalBlock();
                Result = memoryStream.ToArray();
                cryptoStream.Close();
                memoryStream.Close();
                cryptoStream.Dispose();
                memoryStream.Dispose();

            }
            catch (Exception)
            {
                Result = null;
            }

            return Result;

        }

        private byte[] Decrypt(byte[] EncData)
        {

            byte[] Result = null;

            try
            {
                System.Security.Cryptography.RijndaelManaged Enc = new System.Security.Cryptography.RijndaelManaged();
                Enc.KeySize = 256;
                Enc.Key = this.Encryption_Key();
                Enc.IV = this.Encryption_IV();

                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(EncData);
                System.Security.Cryptography.CryptoStream cryptoStream = null;
                cryptoStream = new System.Security.Cryptography.CryptoStream(memoryStream, Enc.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Read);

                byte[] TempDecryptArr = null;
                TempDecryptArr = new byte[EncData.Length + 1];
                int decryptedByteCount = 0;
                decryptedByteCount = cryptoStream.Read(TempDecryptArr, 0, EncData.Length);

                cryptoStream.Close();
                memoryStream.Close();
                cryptoStream.Dispose();
                memoryStream.Dispose();

                Result = new byte[decryptedByteCount + 1];
                Array.Copy(TempDecryptArr, Result, decryptedByteCount);
            }
            catch (Exception)
            {
                Result = null;
            }

            return Result;

        }

        private byte[] Encryption_Key()
        {

            //Generate a byte array of required length as the encryption key.
            //A SHA256 hash of the passphrase has just the required length. It is used twice in a manner of self-salting.
            System.Security.Cryptography.SHA256Managed SHA256 = new System.Security.Cryptography.SHA256Managed();
            string L1 = System.Convert.ToBase64String(SHA256.ComputeHash(this._TextEncoding.GetBytes(this._Passphrase)));
            string L2 = this.Passphrase + L1;
            byte[] Result = SHA256.ComputeHash(this._TextEncoding.GetBytes(L2));
            return Result;

        }

        private byte[] Encryption_IV()
        {

            //Generate a byte array of required length as the iv.
            //A MD5 hash of the passphrase has just the required length. It is used twice in a manner of self-salting.
            System.Security.Cryptography.MD5CryptoServiceProvider MD5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            string L1 = System.Convert.ToBase64String(MD5.ComputeHash(this._TextEncoding.GetBytes(this._Passphrase)));
            string L2 = this.Passphrase + L1;
            byte[] Result = MD5.ComputeHash(this._TextEncoding.GetBytes(L2));
            return Result;

        }

    }
}
