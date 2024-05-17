import { LitElement, TemplateResult, css, html } from 'lit';
import { customElement } from 'lit/decorators.js';

import { repeat } from 'lit/directives/repeat.js';

// Polyfills:
import 'reflect-metadata';
import 'urlpattern-polyfill';

// import './menu-view';
import { map } from 'rxjs';
import Container from 'typedi';
import LogState, {
  ErrorMessage,
  LogMessage,
  Message,
} from '../state/Log.state';
import { observe } from '../utils/lit.utils';
import './player-view';

const logState = Container.get(LogState);

// @ts-ignore
console.stdLog = console.log.bind(console);
console.log = function () {
  // @ts-ignore
  logState.logMany(arguments);
  // @ts-ignore
  console.stdLog.apply(console, arguments);
};

// @ts-ignore
console.stdError = console.error.bind(console);
console.error = function () {
  // @ts-ignore
  logState.errorMany(...arguments);
  // @ts-ignore
  console.stdError.apply(console, arguments);
};

type MessageBuilder<T extends Message['type']> = (
  message: Extract<Message, { type: T }>
) => TemplateResult;

type MessageTemplateLookup = {
  [K in Message['type']]: MessageBuilder<K>;
};

// Helper type func which takes a Message type and returns it casted as a concrete type (LogMessage or ErrorMessage)
// const asConcreteType = <T extends Message['type']>(message: Message, type: T) =>
//   message as Extract<Message, { type: T }>;

const formatTimestamp = (timestamp: number) => {
  const date = new Date(timestamp);
  // Example: [04:00:00.000 AM]
  return `[${date.toLocaleTimeString('en-US', {
    hour12: true,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    // @ts-ignore
    fractionalSecondDigits: 3,
  })}]:`;
};

@customElement('app-root')
export class AppComponent extends LitElement {
  messageTemplateLookup: MessageTemplateLookup = {
    log: (log: LogMessage) =>
      html`<div class="log-normal log">
        <div class="log-timestamp">${formatTimestamp(log.timestamp)}</div>
        ${log.text}
      </div>`,
    error: (error: ErrorMessage) =>
      html`<div class="log-error log">
        <div class="log-timestamp">${formatTimestamp(error.timestamp)}</div>
        ${error.text}
      </div>`,
  };

  messages = logState.Messages.pipe(
    map((ml) => ml.sort((a, b) => a.timestamp - b.timestamp).reverse()),
    map(
      (ml) =>
        html`${repeat(
          ml,
          (m) => m.timestamp,
          (m) => this.messageTemplateLookup[m.type](m as any)
        )}`
    )
  );

  // private menuRoute = {
  //   render: () => html`<menu-view></menu-view>`,
  // enter: async () => {
  //   await import('./menu-view');
  //   return true;
  // },
  // };

  // private router: Router = new Router(this, [
  //   {
  //     path: '/',
  //     render: () => html`<player-view></player-view>`,
  //     enter: async () => {
  //       await import('./player-view');
  //       return true;
  //     },
  //   },
  //   {
  //     path: '/menu/*',
  //     ...this.menuRoute,
  //   },
  //   {
  //     path: '/menu',
  //     ...this.menuRoute,
  //   },
  // ]);

  render() {
    // return html`${this.router.outlet()}`;
    return html`<div class="logs">${observe(this.messages)}</div>
      <player-view></player-view>`;
  }

  static styles = [
    css`
      :host {
        display: flex;
        flex: 1;
        flex-direction: column;
      }

      .logs {
        display: flex;
        flex-direction: column;
        padding: 1rem;
        background-color: #333;
        color: white;
        overflow-y: auto;
        max-height: 25vh;

        /* keep scroll area at the bottom when new items come it */
        flex-direction: column-reverse;
      }

      /* Animate .log so they fade in when new ones showup */
      .log {
        animation: fadeIn 0.5s;
      }

      @keyframes fadeIn {
        from {
          opacity: 0;
        }
        to {
          opacity: 1;
        }
      }

      .log {
        display: flex;
        padding: 0.5rem;
        border-radius: 0.5rem;
      }

      .log-timestamp {
        color: #999;
        margin-right: 0.2rem;
      }

      .log-normal {
        color: white;
      }

      /* tinted red background, rounded corners, tinted red text. Following material 3 guidelines */
      .log-error {
        background-color: #f4433644;
        border: 1px solid #f44336;
        color: #eebcb8;
      }
    `,
  ];
}
