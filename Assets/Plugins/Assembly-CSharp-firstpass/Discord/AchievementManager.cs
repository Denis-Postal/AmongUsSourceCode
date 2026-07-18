using System;
using System.Runtime.InteropServices;

namespace Discord
{
	public class AchievementManager
	{
		internal struct FFIEvents
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void UserAchievementUpdateHandler(IntPtr ptr, ref UserAchievement userAchievement);

			internal UserAchievementUpdateHandler OnUserAchievementUpdate;
		}

		internal struct FFIMethods
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SetUserAchievementCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SetUserAchievementMethod(IntPtr methodsPtr, long achievementId, long percentComplete, IntPtr callbackData, SetUserAchievementCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchUserAchievementsCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchUserAchievementsMethod(IntPtr methodsPtr, IntPtr callbackData, FetchUserAchievementsCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void CountUserAchievementsMethod(IntPtr methodsPtr, ref int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetUserAchievementMethod(IntPtr methodsPtr, long userAchievementId, ref UserAchievement userAchievement);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetUserAchievementAtMethod(IntPtr methodsPtr, int index, ref UserAchievement userAchievement);

			internal SetUserAchievementMethod SetUserAchievement;

			internal FetchUserAchievementsMethod FetchUserAchievements;

			internal CountUserAchievementsMethod CountUserAchievements;

			internal GetUserAchievementMethod GetUserAchievement;

			internal GetUserAchievementAtMethod GetUserAchievementAt;
		}

		public delegate void SetUserAchievementHandler(Result result);

		public delegate void FetchUserAchievementsHandler(Result result);

		public delegate void UserAchievementUpdateHandler(ref UserAchievement userAchievement);

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

		public event UserAchievementUpdateHandler OnUserAchievementUpdate;

		internal AchievementManager(IntPtr ptr, IntPtr eventsPtr, ref FFIEvents events)
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
			events.OnUserAchievementUpdate = delegate(IntPtr ptr, ref UserAchievement userAchievement)
			{
				if (this.OnUserAchievementUpdate != null)
				{
					this.OnUserAchievementUpdate(ref userAchievement);
				}
			};
			Marshal.StructureToPtr(events, eventsPtr, fDeleteOld: false);
		}

		public void SetUserAchievement(long achievementId, long percentComplete, SetUserAchievementHandler callback)
		{
			FFIMethods.SetUserAchievementCallback setUserAchievementCallback = delegate(IntPtr ptr, Result result)
			{
				Utility.Release(ptr);
				callback(result);
			};
			Methods.SetUserAchievement(MethodsPtr, achievementId, percentComplete, Utility.Retain(setUserAchievementCallback), setUserAchievementCallback);
		}

		public void FetchUserAchievements(FetchUserAchievementsHandler callback)
		{
			FFIMethods.FetchUserAchievementsCallback fetchUserAchievementsCallback = delegate(IntPtr ptr, Result result)
			{
				Utility.Release(ptr);
				callback(result);
			};
			Methods.FetchUserAchievements(MethodsPtr, Utility.Retain(fetchUserAchievementsCallback), fetchUserAchievementsCallback);
		}

		public int CountUserAchievements()
		{
			int count = 0;
			Methods.CountUserAchievements(MethodsPtr, ref count);
			return count;
		}

		public UserAchievement GetUserAchievement(long userAchievementId)
		{
			UserAchievement userAchievement = default(UserAchievement);
			Result result = Methods.GetUserAchievement(MethodsPtr, userAchievementId, ref userAchievement);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return userAchievement;
		}

		public UserAchievement GetUserAchievementAt(int index)
		{
			UserAchievement userAchievement = default(UserAchievement);
			Result result = Methods.GetUserAchievementAt(MethodsPtr, index, ref userAchievement);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return userAchievement;
		}
	}
}
