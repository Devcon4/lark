import { LitElement, css, html } from 'lit';

import '@material/web/button/filled-tonal-button.js';
import { customElement } from 'lit/decorators.js';

@customElement('settings-view')
export default class SettingsView extends LitElement {
  render() {
    return html`<div class="settings">
      <md-filled-tonal-button href="/menu/">Back</md-filled-tonal-button>
      <md-filled-tonal-button>Apply</md-filled-tonal-button>
    </div>`;
  }

  static styles = css`
    :host {
      display: flex;
      flex: 1;
      justify-content: center;
      align-items: center;
    }

    .settings {
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
