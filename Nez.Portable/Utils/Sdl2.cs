using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Nez {
	public static class Sdl2 {
		private static IntPtr sdl;

		static Sdl2() {
			Initialize();
			BindMethods();
		}

		private static void Initialize() {
			var mg = typeof(Game).Assembly;
			var mgSdl = mg.GetType("Sdl");
			sdl = (IntPtr) mgSdl.GetField("NativeLibrary").GetValue(null);
		}

		private static void BindMethods() {
			SDL_GetError = Native.LoadFunction<d_sdl_geterror>(sdl, nameof(SDL_GetError));
			SDL_SetClipboardText = Native.LoadFunction<d_sdl_setclipboardtext>(sdl, nameof(SDL_SetClipboardText));
			SDL_GetClipboardText = Native.LoadFunction<d_sdl_getclipboardtext>(sdl, nameof(SDL_GetClipboardText));
			SDL_setenv = Native.LoadFunction<d_sdl_setenv>(sdl, nameof(SDL_setenv));
			SDL_getenv = Native.LoadFunction<d_sdl_getenv>(sdl, nameof(SDL_getenv));
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr d_sdl_geterror();

		public static d_sdl_geterror SDL_GetError;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int d_sdl_setclipboardtext(string text);

		public static d_sdl_setclipboardtext SDL_SetClipboardText;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate string d_sdl_getclipboardtext();

		public static d_sdl_getclipboardtext SDL_GetClipboardText;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int d_sdl_setenv(string name, string value, int overwrite);

		public static d_sdl_setenv SDL_setenv;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr d_sdl_getenv(string name);

		private static d_sdl_getenv SDL_getenv;

		public static string SDL_GetEnv(string name) {
			return Native.Utf8ToString(SDL_getenv(name));
		}
	}
}