export type MenuMeal = {
    id: string;
    name: string;
    category: string;
    imageUrl: string;
    price: number;
};

export type Menu = {
    menuID: string;
    menuName: string;
    meals: MenuMeal[];
};
