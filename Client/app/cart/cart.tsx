"use client";

import React, {
    createContext,
    JSX,
    useContext,
    useMemo,
    useState
} from "react";
import type { MenuMeal } from "../menu/menu.type";

export type CartItem = {
    id: string;
    name: string;
    price: number;
    quantity: number;
};

type CartContextValue = {
    items: CartItem[];
    totalQuantity: number;
    addMeal: (meal: MenuMeal) => void;
    increment: (mealID: string) => void;
    decrement: (mealID: string) => void;
    clear: () => void;
};

const CartContext = createContext<CartContextValue | undefined>(undefined);

type CartProviderProps = {
    children: React.ReactNode;
};

export function CartProvider(props: CartProviderProps): JSX.Element {
    const [items, setItems] = useState<CartItem[]>([]);

    const addMeal = (meal: MenuMeal): void => {
        setItems((previousItems: CartItem[]): CartItem[] => {
            const existing: CartItem | undefined = previousItems.find((x: CartItem): boolean => x.id === meal.id);
            if (existing === undefined) {
                const newItem: CartItem = {
                    id: meal.id,
                    name: meal.name,
                    price: meal.price,
                    quantity: 1
                };

                return [...previousItems, newItem];
            }

            const updated: CartItem[] = previousItems.map((x: CartItem): CartItem => {
                if (x.id === meal.id) {
                    return {
                        ...x,
                        quantity: x.quantity + 1
                    };
                }

                return x;
            });

            return updated;
        });
    };

    const increment = (mealID: string): void => {
        setItems((previousItems: CartItem[]): CartItem[] => {
            const existing: CartItem | undefined = previousItems.find((x: CartItem): boolean => x.id === mealID);
            if (existing === undefined) {
                return previousItems;
            }

            const updated: CartItem[] = previousItems.map((x: CartItem): CartItem => {
                if (x.id === mealID) {
                    return {
                        ...x,
                        quantity: x.quantity + 1
                    };
                }

                return x;
            });

            return updated;
        });
    };

    const decrement = (mealID: string): void => {
        setItems((previousItems: CartItem[]): CartItem[] => {
            const existing: CartItem | undefined = previousItems.find((x: CartItem): boolean => x.id === mealID);
            if (existing === undefined) {
                return previousItems;
            }

            if (existing.quantity <= 1) {
                const remaining: CartItem[] = previousItems.filter((x: CartItem): boolean => x.id !== mealID);
                return remaining;
            }

            const updated: CartItem[] = previousItems.map((x: CartItem): CartItem => {
                if (x.id === mealID) {
                    return {
                        ...x,
                        quantity: x.quantity - 1
                    };
                }

                return x;
            });

            return updated;
        });
    };

    const clear = (): void => {
        setItems([]);
    };

    const totalQuantity: number = useMemo((): number => {
        let total: number = 0;

        for (const item of items) {
            total += item.quantity;
        }

        return total;
    }, [items]);

    const value: CartContextValue = {
        items,
        totalQuantity,
        addMeal,
        increment,
        decrement,
        clear
    };

    return (
        <CartContext.Provider value={value}>
            {props.children}
        </CartContext.Provider>
    );
}

export function useCart(): CartContextValue {
    const context: CartContextValue | undefined = useContext(CartContext);
    if (context === undefined) {
        throw new Error("CartContext not found. Wrap components with CartProvider.");
    }

    return context;
}
