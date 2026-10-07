using Microsoft.AspNetCore.Components;
namespace Voxen.Components.Shared;
public abstract class LocalizedComponent : ComponentBase
{
    [CascadingParameter] public string? InterfaceLanguage { get; set; }
}
