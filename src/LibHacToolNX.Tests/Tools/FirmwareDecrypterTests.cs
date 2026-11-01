// using LibHac.Tools.FsSystem.NcaUtils;
// using System;
// using System.IO;
//
// namespace LibHac.Secure.Tests.Tools;
//
// public class FirmwareDecrypterTests : DecrypterTests
// {
//     /// <summary>
//     /// Defines the source root path.
//     /// </summary>
//     protected static readonly string SourceRootPath =
//         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "firmware_19.0.1");
//
//     /// <summary>
//     /// Defines the destination root path.
//     /// </summary>
//     protected static readonly string DestinationRootPath =
//         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "firmware_19.0.1_decrypted");
//     
//     [Test]
//     public void DecryptsTheFirmware()
//     {
//         var keySet = CreateEncryptedKeySet();
//         
//         var source = new DirectoryInfo(SourceRootPath);
//         if (!source.Exists)
//         {
//             Assert.Fail("The source directory does not exist.");
//         }
//         
//         var destination = new DirectoryInfo(DestinationRootPath);
//         if (destination.Exists)
//         {
//             destination.Delete(true);
//         }
//
//         destination.Create();
//         
//         var target = new FirmwareDecrypter(keySet);
//         target.Decrypt(source, destination);
//     }
// }