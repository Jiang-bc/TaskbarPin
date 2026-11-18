using System;
using System.Reflection;
using System.Windows.Forms;

static class TestAccess
{
    internal const string FormTypeName = "TaskbarPin.PinTrayForm";
    internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    internal const string PollTimer = "_pollTimer";
    internal const string TrayIcon = "_trayIcon";

    internal static void Quiet(Form form)
    {
        ((Timer)Get(form, PollTimer)).Stop();
        ((NotifyIcon)Get(form, TrayIcon)).Visible = false;
    }

    static Type TargetType(object target) { return target as Type ?? target.GetType(); }

    internal static object Get(object target, string name)
    {
        return Field(target, name).GetValue(target is Type ? null : target);
    }

    internal static void Set(object target, string name, object value)
    {
        Field(target, name).SetValue(target is Type ? null : target, value);
    }

    static FieldInfo Field(object target, string name)
    {
        Type type = TargetType(target);
        FieldInfo field = type.GetField(name, Members);
        if (field == null) throw new MissingFieldException(type.FullName, name);
        return field;
    }

    internal static object Call(object target, string name, params object[] arguments)
    {
        Type type = TargetType(target);
        foreach (MethodInfo method in type.GetMethods(Members))
            if (method.Name == name && method.GetParameters().Length == (arguments == null ? 0 : arguments.Length))
                return method.Invoke(target is Type ? null : target, arguments);
        throw new MissingMethodException(type.FullName, name);
    }
}
