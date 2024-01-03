using System;
using System.Collections.Generic;


namespace Weave;


public class WeaveInstance {
    public WeaveFileDefinition FileDefinition { get; }

    public WeaveInstance(WeaveFileDefinition fileDefinition) => FileDefinition = fileDefinition;

    public void Invoke(WeaveEventInfo weaveEventInfo) => FileDefinition.Invoke(this, weaveEventInfo);

    public void Invoke<T>(WeaveEventInfo<T> weaveEventInfo, T arg) => FileDefinition.Invoke(this, weaveEventInfo, arg);

    public void Invoke<T1, T2>(WeaveEventInfo<T1, T2> weaveEventInfo, T1 arg1, T2 arg2) => FileDefinition.Invoke(this, weaveEventInfo, arg1, arg2);
}