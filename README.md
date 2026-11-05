# HacToolNX

HacToolNX is an example program that uses LibHac to perform various functions against the supported file types with a user friendly command line interface.

## Usage
```
Usage: hactoolnx.exe command [options...]

Commands:
  decrypt              Decrypts an archive.
  unpack               Unpacks the contents of an archive.
Options:
  --help               Show the help information for a command.
```

## Examples
The following examples show how to use the tool to perform various tasks. For the most up-to-date information for the commands and available options, please use the `--help` option without specifying a command, or on any command for available command options.

#### Decrypting archives
`hactoolnx.exe decrypt -s "<file>" -o "<destination>" -k "<keys>"`

NOTE: If you need the file in a .dnca, .dnsp, or .dxci format, you can use the `--use-alternate-format` option available on the decrypt command.

#### Decrypting firmware
The firmware decryption will also handle rebuilding the digest manifests necessary to pass any validation checks that may be used to verify the firmware.

`hactoolnx.exe decrypt -s "<file>" -o "<destination>" -k "<keys>" --firmware`

NOTE: If you need the firmware in .dnca format, you can use the optional `--use-alternate-format` option available on the decrypt command.

#### Unpacking archives
The following example depicts how to unpack an archive while keeping the internal content structure:

`hactoolnx.exe unpack -s "<file>" -o "<destination>" -k "<keys>"`

The following example depicts how to unpack an archive along with any other archives it finds.

`hactoolnx.exe unpack -s "<file>" -o "<destination>" -k "<keys>" --recursive`

## External Keys

For more detailed information on keyset files, see [Keys.md](docs/Keys.md).

Keys can be loaded from text files by specifying a directory with the `-k` argument. The files should be in the same format read by [hactool](https://github.com/SciresM/hactool#external-keys):  
"Keyset files are text files containing one key per line, in the form "key_name = HEXADECIMALKEY". Case shouldn't matter, nor should whitespace."

## Credits
The code was originally developed by Alex Barney, thank you!
