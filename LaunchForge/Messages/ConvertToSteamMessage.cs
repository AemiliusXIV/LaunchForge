using LaunchForge.ViewModels.Steps;

namespace LaunchForge.Messages;

public record ConvertToSteamMessage(
    LaunchAppStepViewModel SourceVm,
    string                 AppId,
    string                 GameName);
