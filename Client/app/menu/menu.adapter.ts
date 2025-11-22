import { apiGetJson } from "../domain/adapter";
import type { Menu } from "./menu.type";

export async function getMenuIDs(): Promise<string[]> {
    const ids: string[] = await apiGetJson<string[]>("/api/menu/ids");
    return ids;
}

export async function getMenu(menuID: string): Promise<Menu> {
    const menu: Menu = await apiGetJson<Menu>("/api/menu/" + encodeURIComponent(menuID));
    return menu;
}
