import * as React from 'react';
import styles from './Dragging.module.scss';
import { IDraggingProps } from './IDraggingProps';
import { escape, clone, findIndex } from '@microsoft/sp-lodash-subset';

export interface IDraggingState {
  items: IDraggableItem[];
  categories: string[];
}

export interface IDraggableItem {
  name: string;
  category: string;
}

export default class Dragging extends React.Component<IDraggingProps, IDraggingState> {
  constructor(props: IDraggingProps) {
    super(props);
    this.state = {
      categories: ['Column1', 'Column2', 'Column3'],
      items: [
        { name: 'Item1', category: 'Column1' },
        { name: 'Item2', category: 'Column1' },
        { name: 'Item3', category: 'Column2' },
        { name: 'Item4', category: 'Column2' }
      ]
    };
  }
  public render(): React.ReactElement<IDraggingProps> {
    const { categories, items } = this.state;
    return (
      <div className={styles.dragging}>
        <div className={styles.container}>
          <div className={styles.row}>
            {
              categories.map((c: string) => {
                return (<div className={styles.column}
                  onDrop={(event) => this._onDrop(event, c)}
                  onDragOver={this._onDragOver}
                  key={c}>
                  <h2>{c}</h2>
                  <div className={styles.columnitems}>
                    {
                      items
                        .filter((item: IDraggableItem) => item.category === c)
                        .map((i: IDraggableItem) => {
                          return (<div key={i.name} draggable
                            onDragStart={(event) => this._onDragStart(event, i.name)}>
                            {i.name}
                          </div>);
                        })
                    }
                  </div>
                </div>);
              })
            }
          </div>
        </div>
      </div >
    );
  }

  private _onDrop(ev: React.DragEvent<HTMLDivElement>, targetcategory: string): void {
    var data = ev.dataTransfer.getData("Text");
    ev.preventDefault();
    var item = this.state.items.filter((i) => i.name === data);
    if (item.length === 1 && item[0].category !== targetcategory) {
      const newitems = clone(this.state.items);
      const itemindex = findIndex(newitems, i => i.name === data);
      newitems[itemindex].category = targetcategory;
      this.setState({items:newitems});
    }
  }
  private _onDragStart(ev: React.DragEvent<HTMLDivElement>, itemname: string): void {
    ev.dataTransfer.setData("Text", itemname);
  }
  private _onDragOver(ev: React.DragEvent<HTMLDivElement>): void {
    ev.preventDefault();
  }

}
