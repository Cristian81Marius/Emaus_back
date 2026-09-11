namespace Emaus.Api.Dtos.Menu;

public record MenuItemDto(string Key, string Label, string Href, string Icon);

public record MenuSectionDto(string Title, List<MenuItemDto> Items);

/// <summary>`Tabs` are EXACT 4 elemente, ultimul mereu spre Meniu — vezi docs/API.md §11.</summary>
public record MenuConfigDto(List<MenuItemDto> Tabs, List<MenuSectionDto> MoreSections);
