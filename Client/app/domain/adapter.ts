export const apiBaseUrl: string =
    process.env.NODE_ENV === "production"
        ? "https://madengines.xyz/foodstack/service"
        : "http://localhost:5124";

export function apiUrl(path: string): string {
    if (path.startsWith("/") === false) {
        return apiBaseUrl + "/" + path;
    }

    return apiBaseUrl + path;
}

export async function apiGetJson<T>(path: string): Promise<T> {
    const url: string = apiUrl(path);
    const response: Response = await fetch(url);

    if (response.ok === false) {
        throw new Error("Request failed with status " + response.status.toString());
    }

    const data = (await response.json()) as T;
    return data;
}
