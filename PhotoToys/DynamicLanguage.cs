using Microsoft.UI.Xaml.Media;
using PhotoToys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static DynamicLanguage.Extension;
namespace DynamicLanguage;
public class SystemLanguage
{
    public const string? UseDefault = null;
    public static IReadOnlyList<string> Languages { get; } =
#if DEBUG
        new string[]
        {
            "th"
        };
#else
        Windows.System.UserProfile.GlobalizationPreferences.Languages;
#endif
    public static readonly string Error = GetDisplayText(new DisplayTextAttribute(Default: "Error")
    {
        Thai = "Error",
        Sinhala = "දෝෂයකි"
    });
    public static readonly string Okay = GetDisplayText(new DisplayTextAttribute(Default: "Okay")
    {
        Thai = "ตกลง (Okay)",
        Sinhala = "හරි"
    });
    public static readonly string KernelSize = GetDisplayText(new DisplayTextAttribute(Default: "Kernel Size")
    {
        Thai = "ขนาดเคอร์เนล (Kernel Size)"
    });
    public static readonly string StandardDeviation = GetDisplayText(new DisplayTextAttribute(Default: "Standard Deviation")
    {
        Thai = "ส่วนเบี่ยงเบนมาตรฐาน (Standarad Deviation)"
    });
    public static readonly string Intensity = GetDisplayText(new DisplayTextAttribute(Default: "Intensity")
    {
        Thai = "ความเข้ม (Intensity)"
    });
    public static (FontFamily FontFamily, double FontSizeMultiplier)? Font { get; } = new LangSwitchAttribute<(FontFamily, double)?>(Default: default)
    {
        Thai = (new FontFamily("Fonts/TH Sarabun New Regular.ttf#TH Sarabun New"), 1.25)
    }.FinalOutput.Value;
}

public class SystemLanguageLinkAttribute : DisplayTextAttribute
{
    static string GetText(string Name)
    {
        var Member = (FieldInfo) typeof(SystemLanguage).GetMember(Name.Split('.')[^1])[0];

        return Member.GetValue(null)?.ToString() ?? Name.Split('.')[^1].ToReadableName();
    }
    public SystemLanguageLinkAttribute(string NameOfSystemLanguage) : base(Default: GetText(NameOfSystemLanguage))
    {

    }
}

public class DisplayTextAttribute : LangSwitchAttribute<string>
{
    public DisplayTextAttribute(string Default) : base(Default: Default)
    {

    }
}
[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public class LangSwitchAttribute<T> : Attribute
{
    public T Default { get; }
    public Lazy<T> FinalOutput { get; }

    public T? USEnglish { get; set; } = default;
    public T? UKEnglish { get; set; } = default;
    public T? Sinhala { get; set; } = default;
    public T? Thai { get; set; } = default;
    public LangSwitchAttribute(
        T Default
    )
    {
        this.Default = Default;
        FinalOutput = new Lazy<T>(delegate
        {
            T? value = default;
            foreach (var lang in SystemLanguage.Languages)
            {

                value = lang switch
                {
                    "en-US" => USEnglish ?? Default,
                    "en-GB" => UKEnglish ?? Default,
                    "si" => Sinhala,
                    "th" => Thai,
                    _ => default
                };
                if (value != null) return value;
            }
            value = Default;
            return value;
        });
    }
}
static class Extension
{
    public static string GetDisplayText<T>(this T enumValue) where T : Enum
    {
        try
        {
            var enumType = enumValue.GetType();
            var memberInfos = enumType.GetMember(enumValue.ToString());
            var enumValueMemberInfo = memberInfos.First(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo.GetCustomAttributes<DisplayTextAttribute>(false);
            var first = valueAttributes.FirstOrDefault();
            if (first is null) goto End;
            return first.FinalOutput.Value;
        }
        catch
        {
            
        }
    End:
        return enumValue.ToString().ToReadableName();
    }
    public static string GetDisplayText<T>(Expression<Func<T, string?>> member)
    {
        var MemberInfo = member.GetMemberInfo();
        try
        {
            var Attr = MemberInfo.GetCustomAttributes<DisplayTextAttribute>(false).First();
            return Attr.FinalOutput.Value;
        }
        catch
        {
            return MemberInfo.Name.ToReadableName();
        }
    }
    public static string GetDisplayText(DisplayTextAttribute displayTextAttribute)
    {
        return displayTextAttribute.FinalOutput.Value;
    }
    public static string GetDisplayText<T>(string memberName)
    {
        var MemberInfo = typeof(T).GetMember(memberName)[0];
        try
        {
            var Attr = MemberInfo.GetCustomAttributes<DisplayTextAttribute>(false).First();
            return Attr.FinalOutput.Value;
        }
        catch
        {
            return MemberInfo.Name.ToReadableName();
        }
    }
    public static string? GetDisplayText<TSource, TAttr>() where TAttr : DisplayTextAttribute
    {
        try
        {
            var Attr = typeof(TSource).GetCustomAttributes<TAttr>(false).First();
            return Attr.FinalOutput.Value;
        }
        catch
        {
            return null;
        }
    }
    public static string? GetDisplayText<TAttr>(this Type TSource) where TAttr : DisplayTextAttribute
    {
        try
        {
            var Attr = TSource.GetCustomAttributes<TAttr>(false).First();
            return Attr.FinalOutput.Value;
        }
        catch
        {
            return null;
        }
    }
    public static string GetDefaultText<T>(string memberName)
    {
        var MemberInfo = typeof(T).GetMember(memberName)[0];
        try
        {
            var Attr = MemberInfo.GetCustomAttributes<DisplayTextAttribute>(false).First();
            return Attr.Default;
        }
        catch
        {
            return MemberInfo.Name.ToReadableName();
        }
    }
    public static string GetDisplayText<T>()
    {
        try
        {
            var valueAttributes = typeof(T).GetCustomAttributes<DisplayTextAttribute>(false).First();
            return valueAttributes.FinalOutput.Value;
        }
        catch
        {
            return typeof(T).Name.ToReadableName();
        }
    }
    private static MemberInfo GetMemberInfo<TModel, TItem>(this Expression<Func<TModel, TItem>> expr)
    {
        return ((MemberExpression)expr.Body).Member;
    }

}