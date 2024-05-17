import { LitElement, css, html } from 'lit';
import { customElement } from 'lit/decorators.js';

import { Routes } from '@lit-labs/router';
import '@material/web/button/filled-tonal-button.js';
import '@material/web/focus/md-focus-ring.js';

@customElement('menu-view')
export default class MenuView extends LitElement {
  private routes: Routes = new Routes(this, [
    {
      path: '',
      render: () => this.rootView(),
    },
    {
      path: '/',
      render: () => this.rootView(),
    },
    {
      path: 'settings',
      render: () => html`<settings-view></settings-view>`,
      enter: async () => {
        await import('./settings-view');
        return true;
      },
    },
  ]);

  rootView() {
    return html`<div class="menu">
      <md-filled-tonal-button href="../"> Play</md-filled-tonal-button>
      <md-filled-tonal-button href="./settings">
        Settings</md-filled-tonal-button
      >
      <md-filled-tonal-button> Quit</md-filled-tonal-button>
    </div>`;
  }

  render() {
    // Centered menu box with a list of a tag buttons.
    return html`<div class="menu">${this.routes.outlet()}</div>`;
  }

  static styles = css`
    :host {
      display: flex;
      flex: 1;
      justify-content: center;
      align-items: center;
    }

    .menu {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      min-width: 25vw;
      min-height: 75vh;
      padding: 1rem;
      background-color: #333;
      border-radius: 1rem;
    }
  `;
}
