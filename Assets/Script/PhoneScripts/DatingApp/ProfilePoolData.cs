using System;
using System.Collections.Generic;

[Serializable]
public class ProfilePoolData
{
    public List<string> names;
    public List<string> personalityTypes;
    public List<string> bios;
    public int minAge;
    public int maxAge;
}

[Serializable]
public class GeneratedProfile
{
    public string profileName;
    public int age;
    public string bio;
    public string personalityType;
}