import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { ContractKolsRoutingModule } from './contract-kols-routing.module';
import { ContractKolsComponent } from './contract-kols.component';
import { CreateContractKolDialogComponent } from './create-contract-kol/create-contract-kol-dialog.component';
import { EditContractKolDialogComponent } from './edit-contract-kol/edit-contract-kol-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        ContractKolsRoutingModule,
        ContractKolsComponent,
        CreateContractKolDialogComponent,
        EditContractKolDialogComponent,
    ],
})
export class ContractKolsModule {}
