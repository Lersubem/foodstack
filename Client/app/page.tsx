// app/page.tsx
import type { JSX } from "react";
import MenuPageClient from "./menu/page";
import type { Menu } from "./menu/menu.type";

export default async function Page(): Promise<JSX.Element> {
    const emptyMenu: Menu = {
        menuID: "",
        menuName: "",
        meals: []
    };

    return (
        <MenuPageClient menu={emptyMenu} />
    );
}
