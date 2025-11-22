// palette.ts
export type Palette = {
    name: string;
    accentHex: string;
    highlightHex: string;
    backgroundHex: string;
};

export const palettes: Palette[] = [
    {
        name: "Lava",
        accentHex: "#ef4444",   // red
        highlightHex: "#f97316",// orange
        backgroundHex: "#111827"
    },
    {
        name: "Sunset",
        accentHex: "#f97316",   // orange
        highlightHex: "#22c55e",// rose
        backgroundHex: "#111827"
    },
    {
        name: "Amber",
        accentHex: "#f59e0b",   // amber
        highlightHex: "#facc15",// yellow
        backgroundHex: "#1f2937"
    },
    {
        name: "Forest",
        accentHex: "#22c55e",   // green
        highlightHex: "#84cc16",// lime
        backgroundHex: "#052e16"
    },
    {
        name: "Pastel",
        accentHex: "#34d399",   // green
        highlightHex: "#60a5fa",// blue
        backgroundHex: "#0b1120"
    },
    {
        name: "Emerald",
        accentHex: "#10b981",   // emerald
        highlightHex: "#2dd4bf",// teal
        backgroundHex: "#022c22"
    },
    {
        name: "Cyber",
        accentHex: "#22d3ee",   // cyan
        highlightHex: "#a855f7",// purple
        backgroundHex: "#020617"
    },
    {
        name: "Ice",
        accentHex: "#38bdf8",   // cold blue
        highlightHex: "#a5b4fc",// soft indigo
        backgroundHex: "#020617"
    },
    {
        name: "Ocean",
        accentHex: "#0ea5e9",   // sky
        highlightHex: "#22d3ee",// cyan
        backgroundHex: "#020617"
    },
    {
        name: "Grape",
        accentHex: "#a855f7",   // purple
        highlightHex: "#e879f9",// pink
        backgroundHex: "#0f172a"
    },
    {
        name: "Candy",
        accentHex: "#ec4899",   // pink
        highlightHex: "#f97316",// orange
        backgroundHex: "#111827"
    },
    {
        name: "Rose",
        accentHex: "#fb7185",   // rose
        highlightHex: "#facc15",// amber
        backgroundHex: "#111827"
    }

];


export type HslColor = {
    h: number;
    s: number;
    l: number;
};

function normalizeHex(input: string): string {
    let value: string = input.trim();

    if (value.startsWith("#") === true) {
        value = value.substring(1);
    }

    if (value.length === 3) {
        const r: string = value.charAt(0);
        const g: string = value.charAt(1);
        const b: string = value.charAt(2);

        value = r + r + g + g + b + b;
    }

    return value.toLowerCase();
}

export function hexToHsl(hex: string): HslColor {
    const value: string = normalizeHex(hex);

    const rByte: number = parseInt(value.substring(0, 2), 16);
    const gByte: number = parseInt(value.substring(2, 4), 16);
    const bByte: number = parseInt(value.substring(4, 6), 16);

    const r: number = rByte / 255;
    const g: number = gByte / 255;
    const b: number = bByte / 255;

    const max: number = Math.max(r, g, b);
    const min: number = Math.min(r, g, b);

    let h: number = 0;
    let s: number = 0;
    const l: number = (max + min) / 2;

    if (max !== min) {
        const delta: number = max - min;

        if (l > 0.5) {
            s = delta / (2 - max - min);
        } else {
            s = delta / (max + min);
        }

        if (max === r) {
            if (g < b) {
                h = (g - b) / delta + 6;
            } else {
                h = (g - b) / delta;
            }
        } else if (max === g) {
            h = (b - r) / delta + 2;
        } else {
            h = (r - g) / delta + 4;
        }

        h = h * 60;
    }

    const hRounded: number = Math.round(h);
    const sRounded: number = Math.round(s * 100);
    const lRounded: number = Math.round(l * 100);

    const result: HslColor = {
        h: hRounded,
        s: sRounded,
        l: lRounded
    };

    return result;
}

export function applyPaletteToDocument(palette: Palette): void {
    const root: HTMLElement | null = document.documentElement;

    if (root === null) {
        return;
    }

    const accent: HslColor = hexToHsl(palette.accentHex);
    const highlight: HslColor = hexToHsl(palette.highlightHex);
    const background: HslColor = hexToHsl(palette.backgroundHex);

    root.style.setProperty("--palette-accent-h", accent.h.toString());
    root.style.setProperty("--palette-accent-s", accent.s.toString());
    root.style.setProperty("--palette-accent-l", accent.l.toString());

    root.style.setProperty("--palette-highlight-h", highlight.h.toString());
    root.style.setProperty("--palette-highlight-s", highlight.s.toString());
    root.style.setProperty("--palette-highlight-l", highlight.l.toString());

    root.style.setProperty("--palette-background-h", background.h.toString());
    root.style.setProperty("--palette-background-s", background.s.toString());
    root.style.setProperty("--palette-background-l", background.l.toString());
}
