﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;


using AESImplementationSpace;//AES file 2

// Main link between the various structures used.
// Also the used class for external calls
// Utilizes a hybrid EC Diffie-Helman implementation as no particular implementation was specified
namespace ECCoremod
{
  class ECCore
  {
      public KeyContainer c;
      public Cryptor crypt;
      public ECCore()
      {
          c = new KeyContainer();
          crypt = new Cryptor();
      }
      // encrypts and writes the chosen text to the path specified
      public bool Encrypt(string pass, string path)
      {
          bool check = true;
          //byte[] password = Encoding.UTF8.GetBytes(pass);
          crypt.EncryptFile(pass, c.symKey, File.ReadAllBytes(path), path, c.k2.pubKey, c.k1.privKey);
          return check;
      }
      // decrypts this special encrypted version and writes the chosen text to the path specified
      public bool Decrypt(string pass, string path)
      {
          bool check = true;
          //byte[] password = Encoding.UTF8.GetBytes(pass);
          check = crypt.DecryptFile(pass, File.ReadAllBytes(path), path, c.k1.pubKey.Length, c.k2.privKey.Length);
          if (!check)
              Console.WriteLine("failed to decrypt");
          return check;
      }
  }
  // Class to hold keys used for en/decryption
  class KeyContainer
  {
      public KeyHolder k1;
      public KeyHolder k2;
      public byte[] symKey;
      public byte[] symKey2;
      public KeyContainer()
      {
          k1 = new KeyHolder();
          k2 = new KeyHolder();
          symKey = k1.genSharedKey(k1.pair, k2.pubKey);
          symKey2 = k2.genSharedKey(k2.pair, k1.pubKey);

      }
  }
  // Class to generate the keys in a usable format
  class KeyHolder
  {
      public ECDiffieHellmanCng pair;
      public byte[] pubKey;
      public byte[] privKey;

      public KeyHolder()
      {
          pair = new ECDiffieHellmanCng();
          pair.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
          pair.HashAlgorithm = CngAlgorithm.Sha256;
          pubKey = pair.PublicKey.ToByteArray();
          privKey = pair.ExportECPrivateKey();
      }

      public byte[] genSharedKey(ECDiffieHellmanCng a, byte[] secondPubKey)
      {
          byte[] sharedKey = a.DeriveKeyMaterial(CngKey.Import(secondPubKey, CngKeyBlobFormat.EccPublicBlob));
          return sharedKey;
      }


  }
  // an AES en/decryptor for the generated shared key generated by the pub/priv key
  class Cryptor
  {
      AESAltered alt;
      public Cryptor()
      {
          alt = new AESAltered(9999);
          //to do: set this to be able to be goten from process file
      }
      // Writing encryption to file using pair key 1
      // variant that adds the unused pair key 2 for decryption to the file to
      // maintain persistence without generating a new file
      public bool EncryptFile(string pass, byte[] key, byte[]msg, string path, byte[] pubkey, byte[] privkey)
      {
          byte[] ciphertext = Encrypt(key, msg);

          byte[] rv = new byte[privkey.Length + ciphertext.Length + pubkey.Length];
          System.Buffer.BlockCopy(privkey, 0, rv, 0, privkey.Length);
          System.Buffer.BlockCopy(ciphertext, 0, rv, privkey.Length, ciphertext.Length);
          System.Buffer.BlockCopy(pubkey, 0, rv, privkey.Length + ciphertext.Length, pubkey.Length);
          byte[] passcrypt = pcrypt(pass);
          byte[] passblock = new byte[rv.Length + passcrypt.Length];
          System.Buffer.BlockCopy(rv, 0, passblock, 0, rv.Length);
          System.Buffer.BlockCopy(passcrypt, 0, passblock, rv.Length, passcrypt.Length);
         // Console.WriteLine(BitConverter.ToString(passblock));
          File.WriteAllBytes(path, passblock);
          return true;
          //return rv;
      }
      // Parse the keys and cipher text, generate shared key from the keys and
      // decrypt cipher text to file
      public bool DecryptFile(string pass, byte[] cipher, string path, int pubkeysize, int privkeysize)
      {
        //  Console.WriteLine(BitConverter.ToString(cipher));
          byte[] check = pcrypt(pass);
          byte[] pull = new byte[check.Length];
          int size = cipher.Length - pubkeysize - privkeysize - check.Length;
          System.Buffer.BlockCopy(cipher, cipher.Length - check.Length, pull, 0, check.Length);
          if (!Encoding.UTF8.GetString(pull).Equals(Encoding.UTF8.GetString(check)))
              return false;
          KeyHolder proxy = new KeyHolder();
          int tracker = 0;
          byte[] a = new byte[size];
          byte[] privkey = new byte[privkeysize];
          byte[] pubkey = new byte[pubkeysize];
          System.Buffer.BlockCopy(cipher, 0, privkey, 0, privkeysize);
          System.Buffer.BlockCopy(cipher, privkeysize + size ,pubkey, 0, pubkeysize);
          System.Buffer.BlockCopy(cipher, privkeysize, a, 0, size);
          proxy.pair.ImportECPrivateKey(privkey,out tracker);
          byte[] key = proxy.genSharedKey(proxy.pair, pubkey);
          byte[] dec = Decrypt(key , a);
          File.WriteAllBytes(path, dec);
          return true;
          //return dec;
      }
      // Encryption with shared key
      private byte[] Encrypt(byte[] key, byte[]msg)
      {
          alt.setAES(key);
          alt.setAction(AESAltered.ENCRYPT);
          byte[] cipher = alt.ProcessFile(msg);
          return cipher;
      }
      // Decryption with shared key
      private byte[] Decrypt(byte[] key,byte[] ciphermsg)
      {
          alt.setAES(key);
          alt.setAction(AESAltered.DECRYPT);
          byte[] plain = alt.ProcessFile(ciphermsg);
          return plain;
      }

      private byte[] pcrypt(string pass)
      {
          byte[] val = Encoding.UTF8.GetBytes(pass);
          string hex = BitConverter.ToString(val).Replace("-", "");
          int size = hex.Length;
          string newval = "";
          for (int i = 0;i < size; i++)
          {
              newval = newval + hex[i] + "0";
          }
          val = StringToByteArray(newval);
          return val;
      }
      private static byte[] StringToByteArray(String hex)
      {
          int NumberChars = hex.Length;
          byte[] bytes = new byte[NumberChars / 2];
          for (int i = 0; i < NumberChars; i += 2)
              bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
          return bytes;
      }
  }
  // AES from group made AES with variables with adjusted outputs
  // to better suit inputs for ECC usage
  class AESAltered
  {

      private int actionMode;
      private byte[] IV;
      private byte[] counter;
      private AES3481 aes;

      public const int DECRYPT = 0;
      public const int ENCRYPT = 1;

      private const int BLOCK_SIZE = 16;

      public AESWithCTR(string initVector)
      {
          // if(keyIV == null)
          //     keyIV = new Dictionary<string, string>();
          this.path = path;
          this.IV = new byte[BLOCK_SIZE];
          byte[] iv = new byte[BLOCK_SIZE];

          byte[] convertedIV = Encoding.UTF8.GetBytes(initVector);
          byte[] convertedKey = key;
          int keySize = convertedKey.Length;
          if(keySize <= 16)
              keySize = 16;
          else if(keySize <= 24)
              keySize = 24;
          else if (keySize <= 32)
              keySize = 32;
          else
              Console.WriteLine("AES Key cannot be more than 32-byte long.");



          byte[] completeKey = new byte[keySize];
          Array.Copy(convertedKey, 0, completeKey, keySize - convertedKey.Length, convertedKey.Length);
          Array.Copy(convertedIV, 0, iv, BLOCK_SIZE - convertedIV.Length, convertedIV.Length);

          setIV(iv);
          this.aes = new AES3481(keySize);
          aes.setKey(completeKey);
      }
      public void setAES(byte[] key)
      {
          this.aes = new AES3481(32);
          aes.setKey(key);
      }

      public void setAction(int action)
      {
          this.actionMode = action;
      }

      public void setIV(byte[] iv)
      {
          // if (iv.Length != BLOCK_SIZE)
          //     Console.WriteLine("Invalid length of Initial Vector");
          // else
          Array.Copy(iv, 0, this.IV, BLOCK_SIZE - iv.Length, iv.Length);
      }

      private byte[] XORByteArray16(byte[] a, byte[] b)
      {
          byte[] result = new byte[16];

          for(int i = 0; i < BLOCK_SIZE; i++)
              result[i] = (byte)(a[i] ^ b[i]);
          return result;
      }

      private byte[] getUniqueNounceCounter(byte[] iv, byte[] counter)
      {
         return XORByteArray16(iv, counter);
      }

      private void incrementCounter(int index)
      {
          if(index == 0)
              counter[index] += 1;

          if(counter[index] == 0xff)
          {
              counter[index] = 0x0;
              incrementCounter(index - 1);
          }
          else
              counter[index] += 1;

      }

      // Reference: https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.getfiles?view=netcore-3.1
      // Process all files in the directory passed in, recurse on any directories
      // that are found, and process the files they contain.

      private byte[] ProcessFile(byte[] data)
      {
          // Doing encryption or decryption here
          // Console.WriteLine("Processed file '{0}'.", path);

          byte blockFillValue = 0x0f;

          this.counter = new byte[BLOCK_SIZE];
          for(int i = 0; i < BLOCK_SIZE; i++)
              counter[i] = 0x00;
          byte[] content = data;
          byte[] processContent = new byte[content.Length];
          int pointer = 0;
          while (pointer + BLOCK_SIZE <= content.Length - 1)
          {
              byte[] block = new byte[BLOCK_SIZE];
              bool AESfailed = false;
              for(int i = 0; i < BLOCK_SIZE && pointer < content.Length; i++)
              {
                  block[i] = content[pointer];
                  pointer ++;
              }

              byte[] randomCounter = getUniqueNounceCounter(IV, counter);
              incrementCounter(BLOCK_SIZE - 1);
              byte[] aesText = new byte[BLOCK_SIZE];

              if(this.actionMode == ENCRYPT || this.actionMode == DECRYPT)
              {
                  aes.encrypt(randomCounter);
                  aesText = aes.getCipherTextiInBytes();
              }
              else
              {
                  AESfailed = true;
                  Console.WriteLine("AES Failed");
              }
              if(!AESfailed)
                  Array.Copy(XORByteArray16(aesText, block), 0, processContent, pointer - BLOCK_SIZE, BLOCK_SIZE);
          }

          // Check if it's the end of the content (the remainder of content {content.Length mod 16})
          if (pointer < content.Length)
          {
              byte[] tempText = new byte[content.Length + BLOCK_SIZE - (content.Length - pointer)];
              Array.Copy(processContent, tempText, processContent.Length);
              byte[] block = new byte[BLOCK_SIZE];
              bool AESfailed = false;
              // int numOfByteLeft = content.Length - pointer;
              for(int i = 0; i < BLOCK_SIZE; i++)
              {
                  if(pointer + i < content.Length)
                      block[i] = content[pointer + i];
                  else
                      block[i] = blockFillValue;
                  // pointer ++;
              }

              byte[] randomCounter = getUniqueNounceCounter(IV, counter);
              incrementCounter(BLOCK_SIZE - 1);

              byte[] aesText = new byte[BLOCK_SIZE];

              if(this.actionMode == ENCRYPT || this.actionMode == DECRYPT)
              {
                  aes.encrypt(randomCounter);
                  aesText = aes.getCipherTextiInBytes();
              }
              else
              {
                  AESfailed = true;
                  Console.WriteLine("AES Failed");
              }

              // Console.WriteLine(tempText.Length);
              // Console.WriteLine(pointer);

              if(!AESfailed)
                  Array.Copy(XORByteArray16(aesText, block), 0, tempText, pointer, BLOCK_SIZE);
              processContent = new byte[tempText.Length];
              Array.Copy(tempText, processContent, tempText.Length);
          }


          // Only for Decryption
          if(this.actionMode == DECRYPT)
          {
              int p = processContent.Length - 1;
              Boolean checkMark = processContent[p] == blockFillValue;
              for(int i = processContent.Length - 2; i >= 0 && checkMark; i--)
              {
                  if(processContent[i] == blockFillValue)
                  {
                      p = p - 1;
                  }
                  else
                  {
                      checkMark = false;
                  }
              }
              int reducedLength = processContent.Length - (processContent.Length - 1 - p + 1);

              byte[] tempContent = new byte[reducedLength];
              if(reducedLength < processContent.Length)
              {
                  Array.Copy(processContent, tempContent, reducedLength);
                  processContent = new byte[reducedLength];
                  Array.Copy(tempContent, processContent, reducedLength);
              }
          }

          return processContent;
      }


  }
  }}
