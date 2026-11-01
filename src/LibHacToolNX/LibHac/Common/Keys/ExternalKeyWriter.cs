using LibHac.Fs;
using LibHac.Spl;
using LibHac.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RangeType = LibHac.Common.Keys.KeyInfo.KeyRangeType;
using Type = LibHac.Common.Keys.KeyInfo.KeyType;

namespace LibHac.Common.Keys;

public static class ExternalKeyWriter
{
    public static void PrintKeys(SecureKeySet keySet, StringBuilder sb, List<KeyInfo> keys, Type filter, bool onlyPrintDifferentDevKeys)
    {
        if (keys.Count == 0)
            return;

        string devSuffix = onlyPrintDifferentDevKeys ? "_dev" : string.Empty;
        int maxNameLength = keys.Max(x => x.NameLength);
        int currentGroup = 0;

        bool FilterMatches(KeyInfo keyInfo)
        {
            // If we're only printing dev-only keys, skip keys that are the same in both prod and dev environments.
            if (onlyPrintDifferentDevKeys && !keyInfo.Type.HasFlag(Type.DifferentDev))
                return false;

            // A KeyType contains two sub-types that specify how a key is used in the cryptosystem, and whether a key
            // is shared between all consoles or is console-unique. Each of these types are filtered separately.
            // A key must match both of these sub-types to pass through the filter.
            // A value of 0 for a sub-filter means that any value of the filtered sub-type is allowed.
            const Type distributionTypeMask = Type.Common | Type.Device;
            const Type derivationTypeMask = Type.Root | Type.Seed | Type.Derived;

            Type distributionTypeFilter = filter & distributionTypeMask;
            Type derivationTypeFilter = filter & derivationTypeMask;

            bool matchesDistributionTypeFilter = distributionTypeFilter == 0 || (distributionTypeFilter & keyInfo.Type) != 0;
            bool matchesDerivationTypeFilter = derivationTypeFilter == 0 || (derivationTypeFilter & keyInfo.Type) != 0;

            return matchesDistributionTypeFilter && matchesDerivationTypeFilter;
        }

        bool isFirstPrint = true;

        foreach (KeyInfo info in keys.Where(x => x.Group >= 0).Where(FilterMatches)
            .OrderBy(x => x.Group).ThenBy(x => x.Name))
        {
            bool isNewGroup = false;

            if (info.Group == currentGroup + 1)
            {
                currentGroup = info.Group;
            }
            else if (info.Group > currentGroup)
            {
                // Don't update the current group yet because if this key is empty and the next key
                // is in the same group, we need to be able to know to add a blank line before printing it.
                isNewGroup = !isFirstPrint;
            }

            if (info.RangeType == RangeType.Single)
            {
                Span<byte> key = info.Getter(keySet, 0);
                if (key.IsZeros())
                    continue;

                if (isNewGroup)
                {
                    sb.AppendLine();
                }

                string keyName = $"{info.Name}{devSuffix}";

                string line = $"{keyName.PadRight(maxNameLength)} = {key.ToHexString()}";
                sb.AppendLine(line);
                isFirstPrint = false;
                currentGroup = info.Group;
            }
            else if (info.RangeType == RangeType.Range)
            {
                bool hasPrintedKey = false;

                for (int i = info.RangeStart; i < info.RangeEnd; i++)
                {
                    Span<byte> key = info.Getter(keySet, i);
                    if (key.IsZeros())
                        continue;

                    if (hasPrintedKey == false)
                    {
                        if (isNewGroup)
                        {
                            sb.AppendLine();
                        }

                        hasPrintedKey = true;
                    }

                    string keyName = $"{info.Name}{devSuffix}_{i:x2}";

                    string line = $"{keyName.PadRight(maxNameLength)} = {key.ToHexString()}";
                    sb.AppendLine(line);
                    isFirstPrint = false;
                    currentGroup = info.Group;
                }
            }
        }
    }

    public static string PrintTitleKeys(KeySet keySet)
    {
        var sb = new StringBuilder();

        foreach ((RightsId rightsId, AccessKey key) kv in keySet.ExternalKeySet.ToList()
            .OrderBy(x => x.rightsId.ToString()))
        {
            string line = $"{kv.rightsId} = {kv.key}";
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    public static string PrintCommonKeys(SecureKeySet keySet)
    {
        var sb = new StringBuilder();
        PrintKeys(keySet, sb, SecureKeySet.CreateKeyList(), Type.Common, false);
        return sb.ToString();
    }

    public static string PrintDeviceKeys(SecureKeySet keySet)
    {
        var sb = new StringBuilder();
        PrintKeys(keySet, sb, SecureKeySet.CreateKeyList(), Type.Device, false);
        return sb.ToString();
    }

    public static string PrintAllKeys(SecureKeySet keySet)
    {
        var sb = new StringBuilder();
        PrintKeys(keySet, sb, SecureKeySet.CreateKeyList(), 0, false);
        return sb.ToString();
    }

    public static string PrintAllSeeds(SecureKeySet keySet)
    {
        var sb = new StringBuilder();
        PrintKeys(keySet, sb, SecureKeySet.CreateKeyList(), Type.Common | Type.Seed, false);

        if (keySet.CurrentMode == SecureKeySet.Mode.Prod)
        {
            sb.AppendLine();
            keySet.SetMode(SecureKeySet.Mode.Dev);
            PrintKeys(keySet, sb, SecureKeySet.CreateKeyList(), Type.Common | Type.Seed, true);
            keySet.SetMode(SecureKeySet.Mode.Prod);
        }

        return sb.ToString();
    }

    public static string PrintCommonKeysWithDev(SecureKeySet keySet)
    {
        SecureKeySet.Mode originalMode = keySet.CurrentMode;
        var sb = new StringBuilder();

        keySet.SetMode(SecureKeySet.Mode.Prod);
        PrintKeys(keySet, sb, SecureKeySet.CreateKeyList(), Type.Common, false);

        sb.AppendLine();
        keySet.SetMode(SecureKeySet.Mode.Dev);
        PrintKeys(keySet, sb, SecureKeySet.CreateKeyList(), Type.Common, true);

        keySet.SetMode(originalMode);
        return sb.ToString();
    }
}