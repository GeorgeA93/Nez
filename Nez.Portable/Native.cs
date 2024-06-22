using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Nez {
	public static class Native {
		private static PlatformID platform;
		private static MethodInfo loadFunctionInfo;
		private static MethodInfo utf8ToString;

		static Native() {
			Initialize();
		}

		private static void Initialize() {
			platform = Environment.OSVersion.Platform;

			var mg = typeof(Game).Assembly;

			var funcLoaderInfo = mg.GetType("MonoGame.Framework.Utilities.FuncLoader").GetTypeInfo();
			loadFunctionInfo = funcLoaderInfo.GetMethod("LoadFunction");

			var interopHelperInfo = mg.GetType("MonoGame.Framework.Utilities.InteropHelpers").GetTypeInfo();
			utf8ToString = interopHelperInfo.GetMethod("Utf8ToString");
		}

		public static T LoadFunction<T>(IntPtr library, string function) {
			var loadFunctionT = loadFunctionInfo.MakeGenericMethod(typeof(T));
			var res = loadFunctionT.Invoke(null, new object[] {library, function, false});
			// if (res == null) {
			// 	// get error
			// 	var error = dlerror();
			// 	throw new EntryPointNotFoundException($"could not get {function}: {error}");
			// }

			return (T) res;
		}

		public static string Utf8ToString(IntPtr handle) => (string) utf8ToString.Invoke(null, new object[] {handle});

		[Obsolete]
		public static int setenv(string name, string value, bool overwrite = false) {
			var overwriteArg = overwrite ? 1 : 0;
			switch (platform) {
				case PlatformID.Win32NT: return Windows._putenv_s(name, value);
				case PlatformID.MacOSX: return Mac.setenv(name, value, overwriteArg);
				case PlatformID.Unix: return Linux.setenv(name, value, overwriteArg);
			}

			return default;
		}

		[Obsolete]
		public static string getenv(string name) {
			switch (platform) {
				case PlatformID.Win32NT: return Marshal.PtrToStringAnsi(Windows.getenv(name));
				case PlatformID.MacOSX: return Marshal.PtrToStringAnsi(Mac.getenv(name));
				case PlatformID.Unix: return Marshal.PtrToStringAnsi(Linux.getenv(name));
			}

			return default;
		}

		public static class Windows {
			[DllImport("msvcrt.dll")]
			public static extern int _putenv_s(string name, string value);

			[DllImport("msvcrt.dll")]
			public static extern IntPtr getenv(string var);
		}

		public static class Mac {
			[DllImport("/usr/lib/libSystem.dylib")]
			public static extern int setenv(string name, string value, int overwrite);

			[DllImport("/usr/lib/libSystem.dylib")]
			public static extern IntPtr getenv(string var);
		}

		public static class Linux {
			[DllImport("libc", CharSet = CharSet.Ansi)]
			public static extern int setenv(string name, string value, int overwrite);

			[DllImport("libc")]
			public static extern IntPtr getenv(string var);
		}
	}
}