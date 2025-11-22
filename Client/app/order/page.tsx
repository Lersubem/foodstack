// app/order/page.tsx
"use client";

import { Suspense, useEffect, useState } from "react";
import type { JSX } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import styles from "./page.module.scss";
import { getOrderByOrderID } from "./order.adapter";
import type { Order } from "./order.type";

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

function OrderPageContent(): JSX.Element | null {
    const router = useRouter();
    const searchParams = useSearchParams();

    const [order, setOrder] = useState<OrderView | null>(null);

    useEffect((): void => {
        const loadOrder = async (): Promise<void> => {
            try {
                const orderIDParam: string | null = searchParams.get("orderID");

                if (orderIDParam != null && orderIDParam.trim().length > 0) {
                    try {
                        window.localStorage.removeItem("lastOrder");
                    } catch {
                        // ignore storage errors
                    }

                    let apiOrder: Order;

                    try {
                        apiOrder = await getOrderByOrderID(orderIDParam);
                    } catch {
                        router.replace("/");
                        return;
                    }

                    const itemsForView: OrderViewItem[] = apiOrder.request.meals.map((meal): OrderViewItem => {
                        const viewItem: OrderViewItem = {
                            id: meal.id,
                            name: meal.id,
                            price: 0,
                            quantity: meal.quantity
                        };

                        return viewItem;
                    });

                    let total: number = 0;

                    for (const item of itemsForView) {
                        total += item.price * item.quantity;
                    }

                    const view: OrderView = {
                        orderID: apiOrder.orderID,
                        orderTime: apiOrder.orderTime,
                        items: itemsForView,
                        totalAmount: total
                    };

                    setOrder(view);
                    return;
                }

                const raw: string | null = window.localStorage.getItem("lastOrder");

                if (raw === null) {
                    router.replace("/");
                    return;
                }

                const parsed = JSON.parse(raw) as OrderView | null;

                if (parsed == null || parsed.orderID == null || parsed.items == null) {
                    router.replace("/");
                    return;
                }

                if (typeof parsed.totalAmount !== "number") {
                    let total: number = 0;

                    for (const item of parsed.items) {
                        total += item.price * item.quantity;
                    }

                    parsed.totalAmount = total;
                }

                setOrder(parsed);
            } catch {
                router.replace("/");
            }
        };

        loadOrder().catch((): void => {
            router.replace("/");
        });
    }, [router, searchParams]);

    const handlePlaceAnotherOrder = (): void => {
        router.push("/");
    };

    if (order === null) {
        return null;
    }

    return (
        <main className={styles.container}>
            <h1 className={styles.title}>Order confirmation</h1>
            <p className={styles.message}>Order ID: {order.orderID}</p>
            <p className={styles.message}>Placed at: {order.orderTime}</p>

            <section className={styles.items}>
                <h2 className={styles.subTitle}>Ordered items</h2>
                <div className={styles.list}>
                    {order.items.map((item: OrderViewItem): JSX.Element => {
                        const lineTotal: number = item.price * item.quantity;

                        return (
                            <div key={item.id} className={styles.listItem}>
                                <div className={styles.itemInfo}>
                                    <span className={styles.itemName}>{item.name}</span>
                                </div>
                                <div className={`${styles.itemSummary} ${styles.itemRight}`}>
                                    <span className={styles.itemPrice}>€ {item.price.toFixed(2)}</span>
                                    <span className={styles.itemQuantity}>x{item.quantity}</span>
                                    <span className={styles.itemLineTotal}>€ {lineTotal.toFixed(2)}</span>
                                </div>
                            </div>
                        );
                    })}

                    <div className={`${styles.summary} ${styles.listItem}`}>
                        <div className={`${styles.summaryLabel} ${styles.itemInfo}`}>
                            <span>Total:</span>
                        </div>
                        <div className={`${styles.summaryValue} ${styles.itemRight}`}>
                            <span>€ {order.totalAmount.toFixed(2)}</span>
                        </div>
                    </div>
                </div>
            </section>

            <button
                type="button"
                className={`${styles.orderButton} ani`}
                onClick={handlePlaceAnotherOrder}
            >
                Place another order
            </button>
        </main>
    );
}

export default function OrderPage(): JSX.Element {
    return (
        <Suspense fallback={null}>
            <OrderPageContent />
        </Suspense>
    );
}
