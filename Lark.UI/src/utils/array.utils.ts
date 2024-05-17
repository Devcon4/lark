export type KeyOfType<T, V> = keyof {
  [P in keyof T as T[P] extends V ? P : never]: any;
};

export function Group<T, U extends KeyOfType<T, string | number>>(
  arr: T[] | undefined | null,
  selector: U
) {
  type Indexable = string | number;
  let obj: { [x: Indexable]: T[] } = {};
  if (!arr) return arr;

  arr.map(
    (v) =>
      (obj[v[selector] as unknown as Indexable] = [
        ...(obj[v[selector] as unknown as Indexable] || []),
        v,
      ])
  );
  return obj as Record<keyof typeof obj, T[]>;
}
