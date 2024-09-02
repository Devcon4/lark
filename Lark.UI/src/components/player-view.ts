import { Routes } from '@lit-labs/router';
import { LitElement, css, html } from 'lit';
import { customElement } from 'lit/decorators.js';
import './crosshair';
import './health';

import '@material/web/button/filled-tonal-button.js';

@customElement('player-view')
export default class PlayerView extends LitElement {
  private routes = new Routes(this, []);

  render() {
    return html`<div class="header"></div>
      <div class="content">
        <crosshair-component></crosshair-component>
      </div>
      <div class="footer">
        <health-component></health-component>
        <md-filled-tonal-button href=${this.routes.link('/menu/')}
          >Menu</md-filled-tonal-button
        >
      </div>`;
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      flex: 1;
    }

    .content {
      flex: 1;
      display: flex;
      place-items: center;
    }

    .footer {
      display: flex;
      justify-content: space-between;
      background-color: #333;
      opacity: 0.8;

      padding: 1rem;
    }
  `;
}
