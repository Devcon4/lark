import { BehaviorSubject } from 'rxjs';
import { Service } from 'typedi';

@Service()
export default class CounterState {
  public Count = new BehaviorSubject<number | undefined>(undefined);

  public setCount(count: number) {
    this.Count.next(count);
  }

  public incrementCount() {
    this.Count.next((this.Count.value || 0) + 1);
  }
}
