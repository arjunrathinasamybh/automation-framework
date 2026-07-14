namespace O365.Automation.Pages.Login.Steps;

/// <summary>
/// Relative priority of the sign-in step handlers. Only matters when more than one handler reports that
/// its screen is displayed — which is exactly what happens on a rejected credential, where the input
/// screen and the error banner are on the page at the same time.
/// </summary>
internal static class LoginStepOrder
{
    /// <summary>Errors outrank everything: otherwise a rejected password is mistaken for a fresh password prompt.</summary>
    internal const int Error = 0;

    /// <summary>Precedes the username step: the picker must be dismissed before an email field even exists.</summary>
    internal const int AccountPicker = 5;

    internal const int Username = 10;
    internal const int WorkAccountTile = 20;
    internal const int Password = 30;

    /// <summary>
    /// Outranks every other MFA step. When a human is answering the challenge we must not reshape the
    /// screen underneath them — they may be mid-way through approving a push.
    /// </summary>
    internal const int InteractiveMfa = 35;

    /// <summary>Must precede the TOTP step: it is what converts a push-approval screen into a code prompt.</summary>
    internal const int ChooseVerificationMethod = 40;

    internal const int Totp = 50;
    internal const int StaySignedIn = 60;
}
