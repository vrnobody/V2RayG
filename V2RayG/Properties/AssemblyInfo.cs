using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("V2RayG")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("V2RayG")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: InternalsVisibleTo("V2RayG.Tests")]

// 将 ComVisible 设置为 false 会使此程序集中的类型
//对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("7b799000-e68f-450f-84af-5ec9a5eff384")]

// 程序集的版本信息由下列四个值组成: 
//
//      主版本
//      次版本
//      生成号
//      修订号
//
// 可以指定所有值，也可以使用以下所示的 "*" 预置版本号和修订号
// 方法是按如下所示使用“*”: :
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.0.4.3")]
[assembly: AssemblyFileVersion("1.0.0.0")]

/*
 * v0.0.4.3 Revert 09755e5 (from V2RayGCon).
 * v0.0.4.2 upgrade NuGet packages
 * v0.0.4.1 Add Sys:SetTimeout() and task.lua in Luna plug-in.
 * -----------------------------------------------------------
 * v0.0.3.14 Remove coreConfiger:GetHash().
 * v0.0.3.13 Refresh the totals after subscriptions are updated in form option.
 * v0.0.3.12 Reduce the time it takes to remove server.
 * v0.0.3.11 Refactor duplicate checking method.
 * v0.0.3.10 Try to fix dispose exception in form option.
 * v0.0.3.9 Disable unrelated controls when custom PAC is checked in ProxySetter plug-in.
 * v0.0.3.8 Fix serde unicode string throw exception bug.
 * v0.0.3.7 Dispose ACMs.
 * v0.0.3.6 Dispose removed controls in FlyServer.
 *          Remove icon cache in Notifier.
 * v0.0.3.5 Release handle after winform is closed.
 * v0.0.3.4 Set AuxSiWinForm to null after form closed.
 * v0.0.3.3 Add RestartOneServerByUid() in servers service.
 * v0.0.3.2 Dispose lua core after script finished.
 * v0.0.3.1 Use stream in clumsy writer.
 * -----------------------------------------------------------
 * v0.0.2.6 Adjust save user settings interval.
 * v0.0.2.5 Compress plug-ins setting.
 * v0.0.2.4 refactor
 * v0.0.2.3 Add SaveUserSettingsLater() in Luna plug-in.
 *          Change servers setting interval to 60 seconds.
 * v0.0.2.2 Call PerformLayout() after dropdown-menu-item cleared.
 * v0.0.2.1 Remove json related functions in Luna plug-in.
 * -----------------------------------------------------------
 * v0.0.1.8 Use stream in compression.
 * v0.0.1.7 Synchronize logging in Libs.V2Ray.Core.
 * v0.0.1.6 Compress servers config.
 * v0.0.1.5 fix bugs
 * v0.0.1.4 update v2ray-core exit codes
 * v0.0.1.3 add tab-edit in form configer
 * v0.0.1.2 modify global import rules
 * v0.0.1.1 add generate-random-UUID in systray context menu
 * v0.0.1.0 first release
 */
