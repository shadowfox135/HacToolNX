using System;

namespace LibHac.Crypto;

/// <summary>
/// An abstract class which defines basic operations for cryptographic transforms.
/// </summary>
public abstract class SecureCryptoTransform
{
    ~SecureCryptoTransform()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases both managed and optionally unmanaged resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources, <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        // This method intentionally left blank.
    }
}