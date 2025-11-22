// app/layout.tsx
import "./assets/styles/globals.scss";
import type { JSX, ReactNode } from "react";
import { CartProvider } from "./cart/cart";
import "./assets/styles/globals.colors.scss";
import "./assets/styles/fa-glyphs.min.css"
import "./assets/styles/fa.css"

export const metadata = {
    title: "Food menu",
    description: "Simple food ordering frontend"
};

type RootLayoutProps = {
    children: ReactNode;
};

export default function RootLayout(props: RootLayoutProps): JSX.Element {
    return (
        <html lang="en">
            <body>
                <CartProvider>
                    {props.children}
                </CartProvider>
            </body>
        </html>
    );
}
