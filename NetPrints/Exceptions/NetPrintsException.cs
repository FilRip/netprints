using System;

namespace NetPrints.Exceptions
{
    public class NetPrintsException(string message) : Exception(message)
    {
    }
}
