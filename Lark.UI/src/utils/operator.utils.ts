import {
  BehaviorSubject,
  Observable,
  OperatorFunction,
  pipe,
  UnaryFunction,
} from 'rxjs';
import { filter, map } from 'rxjs/operators';
import { Group, KeyOfType } from './array.utils';

function Enabled<U extends { enabled: boolean }>(): OperatorFunction<
  U[] | undefined,
  U[]
> {
  return pipe(
    filter((l) => !!l),
    map((l) => l!.filter((i) => i.enabled))
  );
}

function AsBehaviorSubject<T>(o: Observable<T>) {
  let res = new BehaviorSubject<T | undefined>(undefined);
  o.subscribe((v) => res.next(v));
  return res;
}

function GroupBy<T, U extends KeyOfType<T, string | number>>(
  selector: U
): UnaryFunction<
  Observable<T[] | undefined>,
  Observable<Record<string | number, T[]> | null | undefined>
> {
  return pipe(map((l) => Group(l, selector)));
}

function IsDefinedSingle<T>(): UnaryFunction<
  Observable<T | undefined>,
  Observable<T>
> {
  return pipe(filter((t): t is T => !!t));
}

type UnwrapUndefined<T> = T extends undefined ? never : T;

type Mapped<T extends unknown[]> = {
  [P in keyof T]: UnwrapUndefined<T[P]>;
};

function IsDefinedTuple<T extends [...unknown[]]>() {
  return pipe(filter<T, Mapped<T>>((t): t is Mapped<T> => t.every((v) => !!v)));
}

const CustomOperators = {
  Enabled,
  AsBehaviorSubject,
  GroupBy,
  IsDefinedSingle,
  IsDefinedTuple,
};

export default CustomOperators;
