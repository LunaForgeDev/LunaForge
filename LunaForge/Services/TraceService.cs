using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Services;

public interface ITraceThrowable
{
    
}

public class TraceService
{
    public static TraceService Instance
    {
        get
        {
            field ??= new TraceService();
            return field;
        }
    } = null!;
}
