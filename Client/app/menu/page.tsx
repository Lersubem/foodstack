"use client";

import { CSSProperties, JSX, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import styles from "./page.module.scss";
import { useCart } from "../cart/cart";
import { apiUrl } from "../domain/adapter";
import { getMenu, getMenuIDs } from "./menu.adapter";
import type { Menu, MenuMeal } from "./menu.type";
import type {
    OrderPlacementResult,
    OrderPlacementResultSuccess,
    OrderPlacementResultErrorParametersValidation,
    OrderPlacementResultErrorDuplication,
    OrderPlacementResultErrorMealNotValid,
    OrderRequest,
    OrderRequestItem
} from "../order/order.type";
import { applyPaletteToDocument, hexToHsl, HslColor, Palette, palettes } from "../theme/palette";

type MenuPageClientProps = {
    menu: Menu;
};

type UiErrorState = {
    message: string;
};

type OrderViewItem = {
    id: string;
    name: string;
    price: number;
    quantity: number;
};

type OrderView = {
    orderID: string;
    orderTime: string;
    items: OrderViewItem[];
    totalAmount: number;
};

type MealImageProps = {
    src: string;
    alt: string;
};

function MealImage(props: MealImageProps): JSX.Element {
    const [hasError, setHasError] = useState<boolean>(false);

    const handleError = (): void => {
        setHasError(true);
    };

    if (hasError === true) {
        return (
            <div className={styles.mealImageFallback}>
                <span className="fa fa-diamond-exclamation" aria-hidden="true"></span>
            </div>
        );
    }

    return (
        <img
            src={props.src}
            alt={props.alt}
            className={styles.mealImage}
            onError={handleError}
        />
    );
}

export default function MenuPageClient(props: MenuPageClientProps): JSX.Element {
    const [isSubmitOverlayVisible, setIsSubmitOverlayVisible] = useState<boolean>(false);
    const { items, addMeal, increment, decrement, totalQuantity, clear } = useCart();
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
    const [showHighLoadMessage, setShowHighLoadMessage] = useState<boolean>(false);
    const [error, setError] = useState<UiErrorState | null>(null);
    const [menuIDs, setMenuIDs] = useState<string[]>([]);
    const [menus, setMenus] = useState<Menu[]>(props.menu.menuID === "" ? [] : [props.menu]);
    const [isLoadingMenus, setIsLoadingMenus] = useState<boolean>(false);
    const [isCartPopupVisible, setIsCartPopupVisible] = useState<boolean>(false);
    const router = useRouter();

    useEffect((): void => {
        const sunset: Palette | undefined = palettes.find((x): boolean => x.name === "Sunset");

        if (sunset != null) {
            applyPaletteToDocument(sunset);
        }
    }, []);

    useEffect(() => {
        let isCanceled: boolean = false;

        const loadMenus = async (): Promise<void> => {
            try {
                setIsLoadingMenus(true);

                const ids: string[] = await getMenuIDs();

                if (isCanceled === true) {
                    return;
                }

                setMenuIDs(ids);

                for (const menuID of ids) {
                    try {
                        const menu: Menu = await getMenu(menuID);

                        if ((isCanceled as boolean) === true) {
                            return;
                        }

                        setMenus((previous): Menu[] => {
                            const existing: Menu | undefined = previous.find((x): boolean => x.menuID === menuID);

                            if (existing != null) {
                                return previous;
                            }

                            const updated: Menu[] = [...previous, menu];
                            return updated;
                        });
                    } catch (loadError) {
                        console.error("Failed to load menu", menuID, loadError);
                    }
                }
            } catch (errorUnknown) {
                console.error("Failed to load menus", errorUnknown);

                if (isCanceled === false) {
                    setError({
                        message: "Failed to load menus."
                    });
                }
            } finally {
                if (isCanceled === false) {
                    setIsLoadingMenus(false);
                }
            }
        };

        loadMenus().catch((errorUnknown): void => {
            console.error("Unhandled menu load error", errorUnknown);
        });

        return (): void => {
            isCanceled = true;
        };
    }, []);

    useEffect((): () => void => {
        if (isSubmitting === true) {
            const id: number = window.setTimeout((): void => {
                setIsSubmitOverlayVisible(true);
            }, 0);

            return (): void => {
                window.clearTimeout(id);
            };
        }

        setIsSubmitOverlayVisible(false);

        return (): void => {
            // no-op
        };
    }, [isSubmitting]);

    const handleClickMeal = (meal: MenuMeal): void => {
        setError(null);
        addMeal(meal);
    };

    const handleIncrement = (mealID: string): void => {
        setError(null);
        increment(mealID);
    };

    const handleDecrement = (mealID: string): void => {
        setError(null);
        decrement(mealID);
    };

    const handleRemoveFromCart = (mealID: string, quantity: number): void => {
        setError(null);

        let index: number = 0;

        while (index < quantity) {
            decrement(mealID);
            index += 1;
        }

    };
        
    useEffect((): void => {
        if (items.length === 0 && isCartPopupVisible === true) {
            setIsCartPopupVisible(false);
        }
    }, [items, isCartPopupVisible]);

    const handleCartSummaryClick = (): void => {
        if (totalQuantity === 0) {
            return;
        }

        setIsCartPopupVisible((current: boolean): boolean => {
            setIsThemePopupVisible(false)
            if (current === true) {
                return false;
            }
            updateCartScrollThumb()
            return true;
        });
    };

    const findMealForCart = (mealID: string): MenuMeal | undefined => {
        for (const menu of menus) {
            for (const meal of menu.meals) {
                if (meal.id === mealID) {
                    return meal;
                }
            }
        }

        return undefined;
    };

    const calculateCartTotalAmount = (): number => {
        let total: number = 0;

        for (const item of items) {
            const price: number = item.price;
            const quantity: number = item.quantity;

            total += price * quantity;
        }

        return total;
    };
    const cartTotalAmount: number = calculateCartTotalAmount();

    const cartPopupBodyRef = useRef<HTMLDivElement | null>(null);
    const [cartScrollThumbHeight, setCartScrollThumbHeight] = useState<number>(0);
    const [cartScrollThumbOffset, setCartScrollThumbOffset] = useState<number>(0);
    const updateCartScrollThumb = (): void => {
    const element: HTMLDivElement | null = cartPopupBodyRef.current;

        if (element == null) {
            setCartScrollThumbHeight(0);
            setCartScrollThumbOffset(0);
            return;
        }

        const scrollHeight: number = element.scrollHeight;
        const rect: DOMRect = element.getBoundingClientRect();
        const viewportHeight: number = rect.height;
        const scrollTop: number = element.scrollTop;

        if (scrollHeight <= 0 || viewportHeight <= 0) {
            setCartScrollThumbHeight(0);
            setCartScrollThumbOffset(0);
            return;
        }

        const heightPercent: number = (viewportHeight * 100) / scrollHeight;

        if (heightPercent >= 99) {
            setCartScrollThumbHeight(0);
            setCartScrollThumbOffset(0);
            return;
        }

        const maxScroll: number = scrollHeight - viewportHeight;
        let offsetPercent: number = 0;

        if (maxScroll > 0) {
            offsetPercent = (scrollTop * 100) / maxScroll;
        }
        const slidePercentage = 100 - heightPercent
        offsetPercent = offsetPercent / 100 * slidePercentage

        setCartScrollThumbHeight(heightPercent);
        setCartScrollThumbOffset(offsetPercent);
    };

    const handleCartPopupBodyScroll = (): void => {
        updateCartScrollThumb();
    };

    const handlePlaceOrder = async (): Promise<void> => {
        if (items.length === 0) {
            setError({
                message: "Select at least one meal."
            });
            return;
        }

        if (isSubmitting) {
            return;
        }

        setError(null);
        setShowHighLoadMessage(false);
        setIsSubmitting(true);

        const requestItems: OrderRequestItem[] = items.map((item): OrderRequestItem => {
            const requestItem: OrderRequestItem = {
                id: item.id,
                quantity: item.quantity
            };

            return requestItem;
        });

        const requestID: string = Date.now().toString() + "-" + Math.floor(Math.random() * 1000000).toString();

        const request: OrderRequest = {
            requestID: requestID,
            meals: requestItems
        };

        const url: string = apiUrl("/api/orders");

        const maxAttempts: number = 2;
        let attempt: number = 0;
        let highLoadTimeoutHandle: number | null = null;

        try {
            if (typeof window !== "undefined") {
                highLoadTimeoutHandle = window.setTimeout((): void => {
                    setShowHighLoadMessage(true);
                }, 5000);
            }

            while (attempt < maxAttempts) {
                try {
                    const response: Response = await fetch(url, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json"
                        },
                        body: JSON.stringify(request)
                    });

                    const responseText: string = await response.text();
                    const result = JSON.parse(responseText) as OrderPlacementResult;

                    if (result.status === "Success") {
                        const successResult: OrderPlacementResultSuccess = result;

                        const orderItemsForView: OrderViewItem[] = successResult.order.request.meals.map((requestItem): OrderViewItem => {
                            const cartItem = items.find((x): boolean => x.id === requestItem.id);

                            const name: string = cartItem == null ? requestItem.id : cartItem.name;
                            const price: number = cartItem == null ? 0 : cartItem.price;

                            const viewItem: OrderViewItem = {
                                id: requestItem.id,
                                name: name,
                                price: price,
                                quantity: requestItem.quantity
                            };

                            return viewItem;
                        });

                        let totalAmount: number = 0;

                        for (const item of orderItemsForView) {
                            totalAmount += item.price * item.quantity;
                        }

                        const orderView: OrderView = {
                            orderID: successResult.order.orderID,
                            orderTime: successResult.order.orderTime,
                            items: orderItemsForView,
                            totalAmount: totalAmount
                        };

                        try {
                            window.localStorage.setItem("lastOrder", JSON.stringify(orderView));
                        } catch {
                            // ignore storage errors
                        }

                        clear();
                        setIsCartPopupVisible(false);
                        setError(null);
                        router.push("/order");
                        return;
                    }

                    if (result.status === "InvalidOrderRequest") {
                        const invalidResult: OrderPlacementResultErrorParametersValidation = result;
                        const firstError = invalidResult.errors[0];
                        const message: string = firstError === undefined ? "Order request is not valid." : firstError.message;

                        setError({
                            message
                        });

                        return;
                    }

                    if (result.status === "ExistingOrder" || result.status === "OrderConflict") {
                        const duplicationResult: OrderPlacementResultErrorDuplication = result;

                        if (duplicationResult.hasExistingOrder === true && duplicationResult.existingOrder != null) {
                            const orderID: string = duplicationResult.existingOrder.orderID;
                            router.push("/order?orderID=" + encodeURIComponent(orderID));
                            return;
                        }

                        setError({
                            message: duplicationResult.message ?? "Order already exists or is in conflict."
                        });

                        return;
                    }

                    if (result.status === "MealNotValid") {
                        const mealResult: OrderPlacementResultErrorMealNotValid = result;
                        const firstInvalid: string | undefined = mealResult.invalidMeals[0];

                        if (firstInvalid === undefined) {
                            setError({
                                message: "One or more meals are not valid."
                            });
                            return;
                        }

                        let menuName: string | null = null;

                        for (const menu of menus) {
                            const found = menu.meals.find((m): boolean => m.id === firstInvalid);
                            if (found != null) {
                                menuName = menu.menuName;
                                break;
                            }
                        }

                        let message: string;

                        if (menuName == null || menuName.trim().length === 0) {
                            message = "Meal " + firstInvalid + " is not valid.";
                        } else {
                            message = "Meal " + firstInvalid + " in " + menuName + " is not valid.";
                        }

                        setError({
                            message
                        });

                        return;
                    }
                    setError({
                        message: "Unknown order result status."
                    });
                    return;
                } catch (errorUnknown) {
                    attempt += 1;

                    if (attempt >= maxAttempts) {
                        console.error("Order failed", errorUnknown);

                        setError({
                            message: "We had a technical issue. Please try again later."
                        });

                        return;
                    }
                }
            }
        } finally {
            if (highLoadTimeoutHandle != null && typeof window !== "undefined") {
                window.clearTimeout(highLoadTimeoutHandle);
            }

            setShowHighLoadMessage(false);
            setIsSubmitting(false);
        }
    };

    useEffect((): () => void => {
        if (isCartPopupVisible === false) {
            setCartScrollThumbHeight(0);
            return (): void => {
                // no-op
            };
        }

        const id: number = window.setTimeout((): void => {
            updateCartScrollThumb();
        }, 0);

        return (): void => {
            window.clearTimeout(id);
        };
    }, [items, isCartPopupVisible]);

    const [isThemePopupVisible, setIsThemePopupVisible] = useState<boolean>(false);

    const handleToggleThemePopup = (): void => {
        setIsThemePopupVisible((current: boolean): boolean => {
            setIsCartPopupVisible(false)
            if (current === true) {
                return false;
            }

            return true;
        });
    };

    const handleSelectPalette = (palette: Palette): void => {
        applyPaletteToDocument(palette);
        setIsThemePopupVisible(false);
    };

    return (
        <main className={`${styles.container} ${
                isSubmitting === true || isLoadingMenus === true ? styles.isLoading : ""
            }`}
        >
            <div className={styles.headerBar}>
                <header className={styles.header}>
                    <h1 className={styles.title}>Menu</h1>
                    <button
                        className={`${styles.cartSummary} ani`}
                        onClick={handleCartSummaryClick}
                        disabled={isSubmitting || items.length === 0}
                    >
                        <span className={styles.cartLabel}>Items in cart:</span>
                        <span className={styles.cartCount}>{totalQuantity}</span>
                    </button>
                </header>

                {items.length > 0 && (
                    <div className={`${styles.cartPopup} ${isCartPopupVisible === true ? styles.cartPopupOpen : ""}`}>
                        <div className={styles.cartPopupHeader}>
                            <div className={styles.cartPopupTitle}>
                                Cart
                            </div>
                        </div>
                        <div className={styles.cartPopupBody}
                            ref={cartPopupBodyRef}
                            onScroll={handleCartPopupBodyScroll}
                        >
                            {items.map((item): JSX.Element => {
                                const meal: MenuMeal | undefined = findMealForCart(item.id);

                                const displayName: string = meal == null ? item.name : meal.name;
                                const displayCategory: string = meal == null ? "" : menus.find(m=>m.menuID == meal.category)?.menuName?? meal.category;
                                const imageSrc: string | null = meal == null ? null : apiUrl(meal.imageUrl);

                                return (
                                    <div key={item.id} className={styles.cartItemRow}>
                                        <div className={styles.cartItemRemove}>
                                            <div
                                                className={`${styles.removeButton} ani`}
                                                onClick={(): void => handleRemoveFromCart(item.id, item.quantity)}
                                            >
                                                <span className="fa fa-trash" aria-hidden="true"></span>
                                            </div>
                                        </div>
                                        <div className={styles.mealImageWrapper}>
                                            {imageSrc == null ? (
                                                <div className={styles.mealImageFallback}>
                                                    <span className="fa fa-diamond-exclamation" aria-hidden="true"></span>
                                                </div>
                                            ) : (
                                                <MealImage src={imageSrc} alt={displayName} />
                                            )}
                                        </div>
                                        <div className={styles.cartItemInfo}>
                                            <div className={styles.cartItemName}>
                                                {displayName}
                                            </div>
                                            <div className={styles.cartItemCategory}>
                                                {displayCategory}
                                            </div>
                                        </div>
                                        <div className={styles.quantityControls}>
                                            <button
                                                type="button"
                                                className={`${styles.quantityButton} ani`}
                                                onClick={(): void => handleDecrement(item.id)}
                                            >
                                                -
                                            </button>
                                            <span className={styles.quantityValue}>
                                                {item.quantity}
                                            </span>
                                            <button
                                                type="button"
                                                className={`${styles.quantityButton} ani`}
                                                onClick={(): void => handleIncrement(item.id)}
                                            >
                                                +
                                            </button>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                        <div className={styles.cartTotal}>
                            <span className={styles.cartTotalLabel}>Total:</span>
                            <span className={styles.cartTotalValue}>€ {cartTotalAmount.toFixed(2)}</span>
                        </div>
                        <div className={styles.cartPopupScrollBar}
                            style={{
                                height: cartScrollThumbHeight <= 0 ? "0" : cartScrollThumbHeight.toString() + "%",
                                top: cartScrollThumbOffset.toString() + "%"
                            }}
                        ></div>
                    </div>
                )}

                {error != null && (
                    <div className={styles.error}>
                        {error.message}
                    </div>
                )}
            </div>

            <section className={styles.menuGrid}>
                {isLoadingMenus === true && menuIDs.length === 0 && (
                    <div className={styles.menuLoading}>
                        Loading menus...
                    </div>
                )}

                {menuIDs.map((menuID: string): JSX.Element => {
                    const menu: Menu | undefined = menus.find((x): boolean => x.menuID === menuID);
                    const meals: MenuMeal[] = menu == null ? [] : menu.meals;
                    const menuName: string = menu == null || menu.menuName?.trim()?.length === 0
                        ? menuID
                        : menu.menuName;

                    return (
                        <div key={menuID} className={styles.menuGroup}>
                            <div className={styles.menuHeader}>
                                <div className={styles.menuIdValue}>
                                    {menuName}
                                </div>
                            </div>
                            <div className={styles.menuMeals}>
                                {menu == null && (
                                    <div className={styles.menuLoading}>
                                        <span className={`${styles.menuLoadingGlyph} fa fa-clock-rotate-left`}></span>
                                        <span>Loading meals...</span>
                                    </div>
                                )}

                                {menu != null && meals.length === 0 && (
                                    <div className={styles.menuEmpty}>
                                        No meals available.
                                    </div>
                                )}

                                {meals.map((meal: MenuMeal): JSX.Element => {
                                    const cartItem = items.find((x): boolean => x.id === meal.id);
                                    const quantity: number = cartItem == null ? 0 : cartItem.quantity;

                                    const imageSrc: string = apiUrl(meal.imageUrl);

                                    let actionsContent: JSX.Element;

                                    if (quantity === 0) {
                                        actionsContent = (
                                            <>
                                                <button
                                                    type="button"
                                                    className={`${styles.mealAddButton} ani`}
                                                    onClick={(e): void => { e.stopPropagation(); handleClickMeal(meal); }}
                                                >
                                                    Add
                                                </button>
                                            </>
                                        );
                                    } else {
                                        actionsContent = (
                                            <>
                                                <div className={styles.quantityControls}>
                                                    <button
                                                        type="button"
                                                        className={`${styles.quantityButton} ani`}
                                                        onClick={(e): void => { e.stopPropagation(); handleDecrement(meal.id); }}
                                                    >
                                                        -
                                                    </button>
                                                    <span className={styles.quantityValue}>{quantity}</span>
                                                    <button
                                                        type="button"
                                                        className={`${styles.quantityButton} ani`}
                                                        onClick={(e): void => { e.stopPropagation(); handleIncrement(meal.id); }}
                                                    >
                                                        +
                                                    </button>
                                                </div>
                                            </>
                                        );
                                    }

                                    return (
                                        <article
                                            key={meal.id}
                                            className={`${styles.mealCard} ani`}
                                            onClick={(): void => handleClickMeal(meal)}
                                        >
                                            <div className={styles.mealImageWrapper}>
                                                <MealImage src={imageSrc} alt={meal.name} />
                                            </div>
                                            <div className={styles.mealBody}>
                                                <div className={styles.mealContent}>
                                                    <h2 className={styles.mealName}>{meal.name}</h2>
                                                    <p className={styles.mealCategory}>{menuName}</p>
                                                    <p className={styles.mealPrice}>€ {meal.price.toFixed(2)}</p>
                                                </div>
                                                <div className={styles.mealActions}>
                                                    {actionsContent}
                                                </div>
                                            </div>
                                        </article>
                                    );
                                })}
                            </div>
                        </div>
                    );
                })}
            </section>

            <div className={styles.footerBar}>

                {isThemePopupVisible === true && (
                    <div className={styles.themePopup}>
                        {palettes.map((palette: Palette): JSX.Element => {
                        const accentHsl: HslColor = hexToHsl(palette.accentHex);

                        const paletteStyle = {
                            "--color-accent-h": accentHsl.h.toString(),
                            "--color-accent-s": accentHsl.s.toString(),
                            "--color-accent-l": accentHsl.l.toString()
                        } as CSSProperties;

                        return (
                            <button
                                key={palette.name}
                                type="button"
                                className={`${styles.themePaletteButton} ani`}
                                style={paletteStyle}
                                onClick={(): void => handleSelectPalette(palette)}
                            >
                                <span className={styles.themePaletteName}>{palette.name}</span>
                                <span className={styles.themePaletteSwatches}>
                                    <span className={styles.themeSwatchAccent}></span>
                                    <span className={styles.themeSwatchHighlight}></span>
                                    <span className={styles.themeSwatchBackground}></span>
                                </span>
                            </button>
                        );
                    })}
                    </div>
                )}

                <footer className={styles.footer}>

                    <div className={styles.footerMain}>
                        <button
                            type="button"
                            className={`${styles.themeButton} ani`}
                            onClick={handleToggleThemePopup}
                        >
                            <span className="fa fa-brush"></span>
                        </button>
                    </div>

                    <div className={styles.footerControls}>
                        <div className={styles.footerTotal}>
                            <span className={styles.footerTotalLabel}>Total:</span>
                            <span className={styles.footerTotalValue}>€ {cartTotalAmount.toFixed(2)}</span>
                        </div>

                        <button
                            type="button"
                            className={`${styles.orderButton} ani`}
                            onClick={handlePlaceOrder}
                            disabled={isSubmitting || items.length === 0}
                        >
                            {isSubmitting ? "Placing order..." : "Order"}
                        </button>
                    </div>
                </footer>
            </div>


            {isSubmitting === true && (
                <div className={`${styles.submitOverlay} ${isSubmitOverlayVisible === true ? styles.submitOverlayVisible : ""}`}>
                    <div className={styles.submitOverlayContent}>
                        <div
                            className={`${styles.submitOverlaySpninnerContent}`}
                            aria-hidden="true"
                        >
                            <div
                                className={`${styles.submitOverlaySpninner}`}
                            ></div>
                        </div>
                        <div className={styles.submitOverlayMessage}>
                            {showHighLoadMessage === true
                                ? "We are experiencing high load. Please be patient while we process your order."
                                : ""}
                        </div>
                    </div>
                </div>
            )}
        </main>
    );
}
