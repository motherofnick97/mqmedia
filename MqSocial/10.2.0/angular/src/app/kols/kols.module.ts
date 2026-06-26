import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { KolsRoutingModule } from './kols-routing.module';
import { KolsComponent } from './kols.component';
import { CreateKolDialogComponent } from './create-kol/create-kol-dialog.component';
import { EditKolDialogComponent } from './edit-kol/edit-kol-dialog.component';
import { AddKolToContractsDialogComponent } from './add-to-contracts/add-kol-to-contracts-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        KolsRoutingModule,
        KolsComponent,
        CreateKolDialogComponent,
        EditKolDialogComponent,
        AddKolToContractsDialogComponent,
    ],
})
export class KolsModule {}
