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
    public readonly static IReadOnlyList<string> Languages =
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
    public static readonly FontFamily? Font = new LangSwitchAttribute<FontFamily?>(Default: null)
    {
        Thai = new FontFamily("Pusub")
    }.FinalOutput.Value;
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
            var enumType = typeof(T);
            var memberInfos = enumType.GetMember(enumValue.ToString());
            var enumValueMemberInfo = memberInfos.First(m => m.DeclaringType == enumType);
            var valueAttributes = (DisplayTextAttribute)
                enumValueMemberInfo.GetCustomAttributes(
                typeof(DisplayTextAttribute), false)[0];
            return valueAttributes.FinalOutput.Value;
        }
        catch
        {
            return enumValue.ToString().ToReadableName();
        }
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