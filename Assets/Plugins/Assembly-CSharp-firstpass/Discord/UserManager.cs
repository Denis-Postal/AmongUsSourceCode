using System;
using System.Runtime.InteropServices;

namespace Discord
{
	public class UserManager
	{
		internal struct FFIEvents
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void CurrentUserUpdateHandler(IntPtr ptr);

			internal CurrentUserUpdateHandler OnCurrentUserUpdate;
		}

		internal struct FFIMethods
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetCurrentUserMethod(IntPtr methodsPtr, ref User currentUser);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void GetUserCallback(IntPtr ptr, Result result, ref User user);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void GetUserMethod(IntPtr methodsPtr, long userId, IntPtr callbackData, GetUserCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetCurrentUserPremiumTypeMethod(IntPtr methodsPtr, ref PremiumType premiumType);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result CurrentUserHasFlagMethod(IntPtr methodsPtr, UserFlag flag, ref bool hasFlag);

			internal GetCurrentUserMethod GetCurrentUser;

			internal GetUserMethod GetUser;

			internal GetCurrentUserPremiumTypeMethod GetCurrentUserPremiumType;

			internal CurrentUserHasFlagMethod CurrentUserHasFlag;
		}

		public delegate void GetUserHandler(Result result, ref User user);

		public delegate void CurrentUserUpdateHandler();

		private IntPtr MethodsPtr;

		private object MethodsStructure;

		private FFIMethods Methods
		{
			get
			{
				if (MethodsStructure == null)
				{
					MethodsStructure = Marshal.PtrToStructure(MethodsPtr, typeof(FFIMethods));
				}
				return (FFIMethods)MethodsStructure;
			}
		}

		public event CurrentUserUpdateHandler OnCurrentUserUpdate;

		internal UserManager(IntPtr ptr, IntPtr eventsPtr, ref FFIEvents events)
		{
			if (eventsPtr == IntPtr.Zero)
			{
				throw new ResultException(Result.InternalError);
			}
			InitEvents(eventsPtr, ref events);
			MethodsPtr = ptr;
			if (MethodsPtr == IntPtr.Zero)
			{
				throw new ResultException(Result.InternalError);
			}
		}

		private void InitEvents(IntPtr eventsPtr, ref FFIEvents events)
		{
			events.OnCurrentUserUpdate = delegate
			{
				if (this.OnCurrentUserUpdate != null)
				{
					this.OnCurrentUserUpdate();
				}
			};
			Marshal.StructureToPtr(events, eventsPtr, fDeleteOld: false);
		}

		public User GetCurrentUser()
		{
			User currentUser = default(User);
			Result result = Methods.GetCurrentUser(MethodsPtr, ref currentUser);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return currentUser;
		}

		public void GetUser(long userId, GetUserHandler callback)
		{
			FFIMethods.GetUserCallback getUserCallback = delegate(IntPtr ptr, Result result, ref User user)
			{
				Utility.Release(ptr);
				callback(result, ref user);
			};
			Methods.GetUser(MethodsPtr, userId, Utility.Retain(getUserCallback), getUserCallback);
		}

		public PremiumType GetCurrentUserPremiumType()
		{
			PremiumType premiumType = PremiumType.None;
			Result result = Methods.GetCurrentUserPremiumType(MethodsPtr, ref premiumType);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return premiumType;
		}

		public bool CurrentUserHasFlag(UserFlag flag)
		{
			bool hasFlag = false;
			Result result = Methods.CurrentUserHasFlag(MethodsPtr, flag, ref hasFlag);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return hasFlag;
		}
	}
}
