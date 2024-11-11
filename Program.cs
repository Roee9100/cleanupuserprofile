using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Timers;
public class UserProfileDeletion
{
    private const int WTS_CURRENT_SERVER_HANDLE = 0;
    private const int WTS_CURRENT_SESSION = -1;
    private const int WTS_DISCONNECTED = 0x00000003;
    private const int WTS_ACTIVE = 0x00000000;
    // Import WTS API functions
    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern IntPtr WTSOpenServer(string pServerName);

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern void WTSCloseServer(IntPtr hServer);

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int Reserved,
        int Version,
        out IntPtr ppSessionInfo,
        out int pCount);

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSLogoffSession(
        IntPtr hServer,
        int SessionId,
        bool bWait);

    // Structure to hold session info
    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int SessionId;
        public IntPtr pWinStationName;
        public int State;

    }

        ;
    IntPtr serverHandle ;
    IntPtr pSessionInfo;
    int sessionCount;

    public static void LogOutDisconnectedDomainUsers()
    {

        IntPtr serverHandle = WTSOpenServer(Environment.MachineName);
        IntPtr pSessionInfo = IntPtr.Zero;
        int sessionCount = 0;

        try
        {
            if (UserProfileDeletion.WTS_CURRENT_SERVER_HANDLE == IntPtr.Zero)
            {
                Console.WriteLine("Failed to open server handle.");
                return;
            }

            // Enumerate all sessions on the server
            if (WTSEnumerateSessions(serverHandle, 0, 1, out pSessionInfo, out sessionCount))
            {
                int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                IntPtr currentSession = pSessionInfo;

                for (int i = 0; i < sessionCount; i++)
                {

                    
                    WTS_SESSION_INFO sessionInfo = Marshal.PtrToStructure<WTS_SESSION_INFO>(currentSession);

                    
                    if (sessionInfo.State != WTS_ACTIVE && sessionInfo.SessionId != 0)
                    {
                        Console.WriteLine($"Logging out disconnected session ID: {sessionInfo.SessionId}");
                        // Log off the disconnected session
                        bool result = WTSLogoffSession(serverHandle, sessionInfo.SessionId, true);

                    }

                    // Move pointer to the next session info structure
                    currentSession += dataSize;
                }
            }
            else
            {
                Console.WriteLine($"Failed to enumerate sessions, error code: {Marshal.GetLastWin32Error()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");

        }

        if (pSessionInfo != IntPtr.Zero) WTSFreeMemory(pSessionInfo);
        if (serverHandle != IntPtr.Zero) WTSCloseServer(serverHandle);

        try
        {
            

            DeleteUserProfiles();
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    static void DeleteUserProfiles()
    {
        
        try
        {
            
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserProfile");

            foreach (ManagementObject profile in searcher.Get())
            {
                string sid = profile["SID"]?.ToString();
                string localPath = profile["LocalPath"]?.ToString();
                bool isLoaded = (bool)profile["Loaded"];

                
                if (isLoaded)
                {
                    Console.WriteLine($"Skipping active profile for SID: {sid}");
                    continue;
                }

                
                if (sid == null ||
                    sid.StartsWith("S-1-5-18") || 
                    sid.StartsWith("S-1-5-19") || 
                    sid.StartsWith("S-1-5-20") || 
                    sid.EndsWith("-500") ||        
                    localPath?.EndsWith("User", StringComparison.OrdinalIgnoreCase) == true) // Exclude "User" profile
                {
                    Console.WriteLine($"Skipping excluded or special profile: {localPath} (SID: {sid})");
                    continue;
                }

                try
                {
                    Console.WriteLine($"Attempting to delete profile: {localPath} (SID: {sid})");
                    profile.Delete();
                    Console.WriteLine($"Successfully deleted profile: {localPath} (SID: {sid})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete profile: {localPath} (SID: {sid}), error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing user profiles: {ex.Message}");
        }
        

    }

    static void Main()
    {
       


        LogOutDisconnectedDomainUsers();

       
    }
}








