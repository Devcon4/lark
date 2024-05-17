import { AsyncDirective } from 'lit/async-directive.js';
import { directive } from 'lit/directive.js';
import { Observable, Subscription } from 'rxjs';

class ObserveDirective extends AsyncDirective {
  #subscription?: Subscription = undefined;

  render(observable: Observable<unknown>) {
    this.#subscription = observable.subscribe((value) => this.setValue(value));
    return ``;
  }

  disconnected() {
    this.#subscription?.unsubscribe();
  }
}

export const observe = directive(ObserveDirective);
