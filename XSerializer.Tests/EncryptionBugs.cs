﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using XSerializer.Encryption;
using XSerializer.Tests.Encryption;

namespace XSerializer.Tests
{
    public class EncryptionBugs
    {
        static EncryptionBugs()
        {
            EncryptionMechanism.Current = new Base64EncryptionMechanism();
        }

        private static readonly IEncryptionMechanism _encryptionMechanism = new EncryptionMarker();

        public class Foo
        {
            [Encrypt]
            public string Bar { get; set; }
        }
        
        public class Baz
        {
            public Baz()
            {
                Quxes = new List<Qux>();
            }

            public List<Qux> Quxes { get; set; }
        }

        [Encrypt]
        public class Qux
        { 
            public int? Grault { get; set; }
        }

        [Test]
        public void EmptyElementWithPropertyMarkedWithEncryptAttributeDoesNotThrow()
        {
            const string xml = @"<Foo><Bar></Bar></Foo>";

            var serializer = new XmlSerializer<Foo>(x => x
                .WithEncryptionMechanism(_encryptionMechanism)
                .WithEncryptKey(typeof(Foo)));

            Assert.That(() => serializer.Deserialize(xml), Throws.Nothing);
        }

        [Test]
        public void ClassMarkedWithEncryptAttributeWithNullablePropertySetToNullDoesNotThrowOnDeserialization()
        {
            var serializer = new XmlSerializer<Baz>(x => x
                .WithEncryptionMechanism(_encryptionMechanism)
                .WithEncryptKey(typeof(Baz)));

            var xml = serializer.Serialize(new Baz { Quxes = { new Qux { Grault = null } } });

            Assert.That(() => serializer.Deserialize(xml), Throws.Nothing);
        }

        public class EncryptionMarker : IEncryptionMechanism
        {
            public string Encrypt(string plainText)
            {
                return "ENCRYPTED(" + plainText
                    .Replace("[", @"\[").Replace('<', '[')
                    .Replace("]", @"\]").Replace('>', ']') + ")";
            }

            string IEncryptionMechanism.Encrypt(string plainText, object encryptKey, SerializationState serializationState)
            {
                if (serializationState == null) throw new ArgumentNullException(nameof(serializationState));
                return Encrypt(plainText);
            }

            public string Decrypt(string cipherText)
            {
                return cipherText.Substring(10, cipherText.Length - 11)
                    .Replace(@"\[", "!!11@@22##33").Replace('[', '<').Replace("!!11@@22##33", "[")
                    .Replace(@"\]", "!!11@@22##33").Replace(']', '>').Replace("!!11@@22##33", "]");
            }

            string IEncryptionMechanism.Decrypt(string cipherText, object encryptKey, SerializationState serializationState)
            {
                if (serializationState == null) throw new ArgumentNullException(nameof(serializationState));
                return Decrypt(cipherText);
            }
        }
    }
}
